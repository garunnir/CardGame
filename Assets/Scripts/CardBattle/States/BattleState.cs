using CardGame.CardBattle.Core;
using Cysharp.Threading.Tasks;

namespace CardGame.CardBattle.States
{
    /// <summary>데미지·반격·사망·대기열 보충·승패 판정.</summary>
    public sealed class BattleState : BaseState
    {
        private readonly bool advanceToEnemyTurn;

        public BattleState(IBattleContext context, bool advanceToEnemyTurnAfter) : base(context)
        {
            advanceToEnemyTurn = advanceToEnemyTurnAfter;
        }

        public override BattleFlowStateId StateId => BattleFlowStateId.Battle;

        public override void Enter()
        {
            ResolveAsync().Forget();
        }

        private async UniTaskVoid ResolveAsync()
        {
            await Context.ExecuteBattleAsync(Context.PendingAction);

            if (Context.CurrentStateId == BattleFlowStateId.GameOver)
            {
                return;
            }

            if (advanceToEnemyTurn)
            {
                Context.ChangeState(new EnemyTurnState(Context));
            }
            else
            {
                Context.ChangeState(new PlayerTurnState(Context));
            }
        }
    }
}
