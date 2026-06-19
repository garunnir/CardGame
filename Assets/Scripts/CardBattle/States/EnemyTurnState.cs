using CardGame.CardBattle.AI;
using CardGame.CardBattle.Cards;
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
            await TurnStartFlow.RunAsync(
                Context,
                Context.Field.EnemyBattlefield,
                isPlayerTurn: false,
                generation);

            if (!IsTransitionCurrent(generation))
            {
                return;
            }

            await RunEnemyTurnAsync(generation);
        }

        private async UniTask RunEnemyTurnAsync(int generation)
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

                var actions = EnemyAIController.BuildTurnActions(
                    Context.Field.EnemyBattlefield,
                    Context.Field.PlayerBattlefield);

                if (actions.Count > 0)
                {
                    var finished = await Context.ExecuteBattleAsync(actions[0]);
                    if (!finished || !IsTransitionCurrent(generation))
                    {
                        return;
                    }
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
    }
}
