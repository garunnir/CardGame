using System.Collections.Generic;
using CardGame.CardBattle.AI;
using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Core;
using Cysharp.Threading.Tasks;

namespace CardGame.CardBattle.States
{
    /// <summary>적 AI 턴 — 전장 적 카드 순차 공격.</summary>
    public sealed class EnemyTurnState : BaseState
    {
        private bool running;

        public EnemyTurnState(GameManager context) : base(context)
        {
        }

        public override BattleFlowStateId StateId => BattleFlowStateId.EnemyTurn;

        public override void Enter()
        {
            Context.IsPlayerTurn = false;
            Context.InputProvider.SetEnabled(false);
            TurnStartHealEffect.Apply(Context.Field.EnemyBattlefield);
            Context.SyncAllViews();
            Context.RaiseTurnBanner(false);
            Context.RaiseHealerPulse();
            RunEnemyTurnAsync().Forget();
        }

        private async UniTaskVoid RunEnemyTurnAsync()
        {
            if (running)
            {
                return;
            }

            running = true;

            var actions = EnemyAIController.BuildTurnActions(
                Context.Field.EnemyBattlefield,
                Context.Field.PlayerBattlefield);

            for (var i = 0; i < actions.Count; i++)
            {
                if (Context.CurrentStateId == BattleFlowStateId.GameOver)
                {
                    break;
                }

                var finished = await Context.ExecuteBattleAsync(actions[i]);
                if (!finished)
                {
                    break;
                }
            }

            running = false;

            if (Context.CurrentStateId != BattleFlowStateId.GameOver)
            {
                Context.ChangeState(new PlayerTurnState(Context));
            }
        }
    }
}
