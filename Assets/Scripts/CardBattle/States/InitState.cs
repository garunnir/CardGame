using CardGame.CardBattle.Core;
using Cysharp.Threading.Tasks;

namespace CardGame.CardBattle.States
{
    public sealed class InitState : BaseState
    {
        public InitState(IBattleContext context) : base(context)
        {
        }

        public override BattleFlowStateId StateId => BattleFlowStateId.Init;

        public override void Enter()
        {
            EnterAsync().Forget();
        }

        private async UniTaskVoid EnterAsync()
        {
            var generation = Context.StateGeneration;
            Context.Field.Clear();
            Context.HeroArena.Clear();
            Context.Field.DeployInitial(Context.PlayerDeckData, true);
            Context.Field.DeployInitial(Context.EnemyDeckData, false);
            Context.HeroArena.DeployInitial(Context.PlayerHeroData, Context.EnemyHeroData);
            await Context.BuildBoardViewsAsync();

            if (!IsTransitionCurrent(generation))
            {
                return;
            }

            Context.RaiseReserveChanged();
            Context.ChangeState(new PlayerTurnState(Context));
        }
    }
}
