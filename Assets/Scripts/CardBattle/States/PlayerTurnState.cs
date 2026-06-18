using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardGame.CardBattle.States
{
    /// <summary>플레이어 턴 — 카드 선택 후 적 타겟 선택.</summary>
    public sealed class PlayerTurnState : BaseState
    {
        private readonly CardBattleTargetingPolicy targetingPolicy;
        private CardModel selectedAttacker;

        public PlayerTurnState(GameManager context) : base(context)
        {
            targetingPolicy = new CardBattleTargetingPolicy(CanAcceptAsTarget);
        }

        private bool CanAcceptAsTarget(CardModel model)
        {
            return CardTargetingRules.IsFaceUpBattlefieldCard(model, Context.FindView(model));
        }

        public override BattleFlowStateId StateId => BattleFlowStateId.PlayerTurn;

        public override void Enter()
        {
            EnterAsync().Forget();
        }

        private async UniTaskVoid EnterAsync()
        {
            Context.IsPlayerTurn = true;
            TurnStartHealEffect.Apply(Context.Field.PlayerBattlefield);
            await Context.SyncAllViewsAsync();
            Context.InputProvider.SetEnabled(true);
            Context.RaiseTurnBanner(true);
            Context.RaiseHealerPulse();
            Context.InputProvider.CardSelected -= OnCardSelected;
            Context.InputProvider.CardSelected += OnCardSelected;
            Context.InputProvider.CardDragStarted -= OnCardDragStarted;
            Context.InputProvider.CardDragMoved -= OnCardDragMoved;
            Context.InputProvider.CardDragEnded -= OnCardDragEnded;
            Context.InputProvider.CardDragStarted += OnCardDragStarted;
            Context.InputProvider.CardDragMoved += OnCardDragMoved;
            Context.InputProvider.CardDragEnded += OnCardDragEnded;
        }

        public override void Exit()
        {
            Context.InputProvider.CardSelected -= OnCardSelected;
            Context.InputProvider.CardDragStarted -= OnCardDragStarted;
            Context.InputProvider.CardDragMoved -= OnCardDragMoved;
            Context.InputProvider.CardDragEnded -= OnCardDragEnded;
            Context.InputProvider.SetEnabled(false);
            selectedAttacker = null;
            Context.DragTargetingPresenter?.EndDrag();
        }

        private void OnCardSelected(CardModel card)
        {
            if (card == null || !card.IsAlive)
            {
                return;
            }

            if (selectedAttacker == null)
            {
                if (!targetingPolicy.CanStartDrag(card))
                {
                    return;
                }

                selectedAttacker = card;
                Context.RaiseAttackerSelected(card);
                return;
            }

            if (card.IsPlayerTeam)
            {
                if (!targetingPolicy.CanStartDrag(card))
                {
                    return;
                }

                selectedAttacker = card;
                Context.RaiseAttackerSelected(card);
                return;
            }

            if (!targetingPolicy.IsValidHover(selectedAttacker, card))
            {
                return;
            }

            Context.PendingAction = new BattleActionRequest(selectedAttacker, card);
            Context.ChangeState(new BattleState(Context, true));
        }

        private void OnCardDragStarted(CardModel source, Vector2 pointerPosition)
        {
            if (!targetingPolicy.CanStartDrag(source))
            {
                return;
            }

            selectedAttacker = source;
            Context.RaiseAttackerSelected(source);
            var sourceView = Context.FindView(source);
            Context.DragTargetingPresenter?.BeginDrag(
                sourceView != null ? sourceView.ViewTransform : null);
        }

        private void OnCardDragMoved(CardModel source, CardModel hoverTarget, Vector2 pointerPosition)
        {
            if (source == null || selectedAttacker != source)
            {
                return;
            }

            var isValidHover = targetingPolicy.IsValidHover(selectedAttacker, hoverTarget);
            var hoverView = Context.FindView(hoverTarget);
            Context.DragTargetingPresenter?.UpdateDrag(
                pointerPosition,
                hoverView != null ? hoverView.ViewTransform : null,
                isValidHover);
        }

        private void OnCardDragEnded(CardModel source, CardModel dropTarget, Vector2 pointerPosition)
        {
            if (source == null || selectedAttacker != source)
            {
                return;
            }

            Context.DragTargetingPresenter?.EndDrag();

            if (targetingPolicy.TryBuildAction(selectedAttacker, dropTarget, out var action))
            {
                Context.PendingAction = action;
                Context.ChangeState(new BattleState(Context, true));
                return;
            }
        }
    }
}
