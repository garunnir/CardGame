using CardGame.CardBattle.AI;
using CardGame.CardBattle.Core;
using Cysharp.Threading.Tasks;

namespace CardGame.CardBattle.States
{
    /// <summary>적 AI 턴 — 플레이어 턴과 동일하게 1회 공격 후 종료.</summary>
    public sealed class EnemyTurnState : BaseState
    {
        private bool running;

        public EnemyTurnState(IBattleContext context) : base(context)
        {
        }

        public override BattleFlowStateId StateId => BattleFlowStateId.EnemyTurn;

        public override void Enter()
        {
            EnterAsync().Forget();
        }

        private async UniTaskVoid EnterAsync()
        {
            var generation = Context.StateGeneration;
            Context.InputProvider.SetEnabled(false);
            var turnAction = await TurnFlow.RunStartAndDetectAsync(
                Context,
                Context.Field.EnemyBattlefield,
                isPlayerTurn: false,
                generation);

            if (turnAction == null)
            {
                return;
            }

            await RunEnemyTurnAsync(generation, turnAction.Value);
        }

        private async UniTask RunEnemyTurnAsync(int generation, TurnActionEvent turnAction)
        {
            if (running)
            {
                return;
            }

            running = true;
            try
            {
                if (!IsTransitionCurrent(generation)
                    || Context.CurrentStateId == BattleFlowStateId.GameOver)
                {
                    return;
                }

                var finished = turnAction.IsAutomatic
                    ? await TurnActionFlow.RunAutomaticAsync(
                        Context,
                        turnAction,
                        () => IsTransitionCurrent(generation))
                    : await ExecuteEnemyCardAttackAsync();

                if (!finished || !IsTransitionCurrent(generation))
                {
                    return;
                }
            }
            finally
            {
                running = false;

                if (IsTransitionCurrent(generation)
                    && Context.CurrentStateId != BattleFlowStateId.GameOver)
                {
                    Context.ChangeState(new PlayerTurnState(Context));
                }
            }
        }

        private async UniTask<bool> ExecuteEnemyCardAttackAsync()
        {
            var actions = EnemyAIController.BuildTurnActions(
                Context.Field,
                Context.HeroArena,
                Context.Field.EnemyBattlefield,
                Context.Field.PlayerBattlefield);

            if (actions.Count > 0)
            {
                return await Context.ExecuteBattleAsync(actions[0]);
            }

            return true;
        }
    }
}
