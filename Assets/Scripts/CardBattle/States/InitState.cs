using System.Collections.Generic;
using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Core;

namespace CardGame.CardBattle.States
{
    public sealed class InitState : BaseState
    {
        public InitState(GameManager context) : base(context)
        {
        }

        public override BattleFlowStateId StateId => BattleFlowStateId.Init;

        public override void Enter()
        {
            Context.Field.Clear();
            Context.Field.DeployInitial(Context.PlayerDeckData, true);
            Context.Field.DeployInitial(Context.EnemyDeckData, false);
            Context.SyncAllViews();
            Context.RaiseReserveChanged();
            Context.ChangeState(new PlayerTurnState(Context));
        }
    }
}
