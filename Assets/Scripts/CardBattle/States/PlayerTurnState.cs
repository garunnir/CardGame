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
        private CardInstanceId selectedAttackerId;

        public PlayerTurnState(GameManager context) : base(context)
        {
            targetingPolicy = new CardBattleTargetingPolicy(model =>
                CardTargetingRules.IsFaceUpBattlefieldCard(context.Field, model));
        }

        public override BattleFlowStateId StateId => BattleFlowStateId.PlayerTurn;

        public override void Enter()
        {
            EnterAsync().Forget();
        }

        private async UniTaskVoid EnterAsync()
        {
            Context.IsPlayerTurn = true;
            var healEvents = TurnStartHealEffect.Apply(Context.Field.PlayerBattlefield);
            await Context.SyncAllViewsAsync();
            Context.InputProvider.SetEnabled(true);
            Context.RaiseTurnBanner(true);
            Context.RaiseHealerPulse(healEvents);
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
            selectedAttackerId = default;
            Context.DragTargetingPresenter?.EndDrag();
        }

        private bool TryGetSelectedAttacker(out CardModel attacker)
        {
            return Context.TryGetModel(selectedAttackerId, out attacker);
        }

        private void SelectAttacker(CardInstanceId attackerId, CardModel attacker)
        {
            selectedAttackerId = attackerId;
            Context.RaiseAttackerSelected(attacker);
        }

        private void OnCardSelected(CardInstanceId cardId)
        {
            if (!Context.TryGetModel(cardId, out var card) || !card.IsAlive)
            {
                return;
            }

            if (!selectedAttackerId.IsValid)
            {
                if (!targetingPolicy.CanStartDrag(card))
                {
                    return;
                }

                SelectAttacker(cardId, card);
                return;
            }

            if (card.IsPlayerTeam)
            {
                if (!targetingPolicy.CanStartDrag(card))
                {
                    return;
                }

                SelectAttacker(cardId, card);
                return;
            }

            if (!TryGetSelectedAttacker(out var attacker)
                || !targetingPolicy.IsValidHover(attacker, card))
            {
                return;
            }

            Context.PendingAction = new BattleActionRequest(attacker, card);
            Context.ChangeState(new BattleState(Context, true));
        }

        private void OnCardDragStarted(CardInstanceId sourceId, Vector2 pointerPosition)
        {
            if (!Context.TryGetModel(sourceId, out var source)
                || !targetingPolicy.CanStartDrag(source))
            {
                return;
            }

            SelectAttacker(sourceId, source);
            var sourceView = Context.FindView(sourceId);
            Context.DragTargetingPresenter?.BeginDrag(
                sourceView != null ? sourceView.ViewTransform : null);
        }

        private void OnCardDragMoved(CardInstanceId sourceId, CardInstanceId hoverTargetId, Vector2 pointerPosition)
        {
            if (!sourceId.IsValid || selectedAttackerId != sourceId)
            {
                return;
            }

            if (!TryGetSelectedAttacker(out var attacker))
            {
                return;
            }

            Context.TryGetModel(hoverTargetId, out var hoverTarget);
            var isValidHover = targetingPolicy.IsValidHover(attacker, hoverTarget);
            var hoverView = hoverTargetId.IsValid ? Context.FindView(hoverTargetId) : null;
            Context.DragTargetingPresenter?.UpdateDrag(
                pointerPosition,
                hoverView != null ? hoverView.ViewTransform : null,
                isValidHover);
        }

        private void OnCardDragEnded(CardInstanceId sourceId, CardInstanceId dropTargetId, Vector2 pointerPosition)
        {
            if (!sourceId.IsValid || selectedAttackerId != sourceId)
            {
                return;
            }

            if (!TryGetSelectedAttacker(out var attacker))
            {
                return;
            }

            Context.DragTargetingPresenter?.EndDrag();
            Context.TryGetModel(dropTargetId, out var dropTarget);

            if (targetingPolicy.TryBuildAction(attacker, dropTarget, out var action))
            {
                Context.PendingAction = action;
                Context.ChangeState(new BattleState(Context, true));
            }
        }
    }
}
