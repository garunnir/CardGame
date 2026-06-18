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

        public EnemyTurnState(GameManager context) : base(context)
        {
        }

        public override BattleFlowStateId StateId => BattleFlowStateId.EnemyTurn;

        public override void Enter()
        {
            EnterAsync().Forget();
        }

        private async UniTaskVoid EnterAsync()
        {
            Context.IsPlayerTurn = false;
            Context.InputProvider.SetEnabled(false);
            TurnStartHealEffect.Apply(Context.Field.EnemyBattlefield);
            await Context.SyncAllViewsAsync();
            Context.RaiseTurnBanner(false);
            Context.RaiseHealerPulse();
            await RunEnemyTurnAsync();
        }

        private async UniTask RunEnemyTurnAsync()
        {
            if (running)
            {
                return;
            }

            running = true;
            try
            {
                if (Context.CurrentStateId == BattleFlowStateId.GameOver)
                {
                    return;
                }

                var actions = EnemyAIController.BuildTurnActions(
                    Context.Field.EnemyBattlefield,
                    Context.Field.PlayerBattlefield);

                if (actions.Count > 0)
                {
                    var finished = await Context.ExecuteBattleAsync(actions[0]);
                    if (!finished)
                    {
                        return;
                    }
                }
            }
            finally
            {
                running = false;

                if (Context.CurrentStateId != BattleFlowStateId.GameOver)
                {
                    Context.ChangeState(new PlayerTurnState(Context));
                }
            }
        }
    }
}
