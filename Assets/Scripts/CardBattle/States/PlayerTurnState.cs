using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Core;

namespace CardGame.CardBattle.States
{
    /// <summary>플레이어 턴 — 카드 선택 후 적 타겟 선택.</summary>
    public sealed class PlayerTurnState : BaseState
    {
        private CardModel selectedAttacker;

        public PlayerTurnState(GameManager context) : base(context)
        {
        }

        public override BattleFlowStateId StateId => BattleFlowStateId.PlayerTurn;

        public override void Enter()
        {
            Context.IsPlayerTurn = true;
            Context.InputProvider.SetEnabled(true);
            TurnStartHealEffect.Apply(Context.Field.PlayerBattlefield);
            Context.SyncAllViews();
            Context.RaiseTurnBanner(true);
            Context.RaiseHealerPulse();
            Context.InputProvider.CardSelected -= OnCardSelected;
            Context.InputProvider.CardSelected += OnCardSelected;
        }

        public override void Exit()
        {
            Context.InputProvider.CardSelected -= OnCardSelected;
            Context.InputProvider.SetEnabled(false);
            selectedAttacker = null;
        }

        private void OnCardSelected(CardModel card)
        {
            if (card == null || !card.IsAlive)
            {
                return;
            }

            if (selectedAttacker == null)
            {
                if (!card.IsPlayerTeam)
                {
                    return;
                }

                selectedAttacker = card;
                Context.RaiseAttackerSelected(card);
                return;
            }

            if (card.IsPlayerTeam)
            {
                selectedAttacker = card;
                Context.RaiseAttackerSelected(card);
                return;
            }

            Context.PendingAction = new BattleActionRequest(selectedAttacker, card);
            Context.ChangeState(new BattleState(Context, true));
        }
    }
}
