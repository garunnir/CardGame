using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardGame.CardBattle.States
{
    /// <summary>플레이어 턴 — 카드 선택 후 적 타겟 또는 적 영웅 타겟 선택.</summary>
    public sealed class PlayerTurnState : BaseState
    {
        private readonly CardBattleTargetingPolicy targetingPolicy;
        private CardInstanceId selectedAttackerId;

        public PlayerTurnState(IBattleContext context) : base(context)
        {
            targetingPolicy = new CardBattleTargetingPolicy(
                model => CardTargetingRules.IsFaceUpBattlefieldCard(context.Field, model)
                    && context.Field.CanTeamAttack(true));
        }

        public override BattleFlowStateId StateId => BattleFlowStateId.PlayerTurn;

        public override void Enter()
        {
            EnterAsync().Forget();
        }

        private async UniTaskVoid EnterAsync()
        {
            var generation = Context.StateGeneration;
            var turnAction = await TurnFlow.RunStartAndDetectAsync(
                Context,
                Context.Field.PlayerBattlefield,
                isPlayerTurn: true,
                generation);

            if (turnAction == null)
            {
                return;
            }

            if (turnAction.Value.IsAutomatic)
            {
                var finished = await TurnActionFlow.RunAutomaticAsync(
                    Context,
                    turnAction.Value,
                    () => IsTransitionCurrent(generation));
                if (!finished || !IsTransitionCurrent(generation))
                {
                    return;
                }

                Context.ChangeState(new EnemyTurnState(Context));
                return;
            }

            BeginCardAttackInput();
        }

        private void BeginCardAttackInput()
        {
            Context.InputProvider.SetEnabled(true);
            Context.InputProvider.CardSelected -= OnCardSelected;
            Context.InputProvider.CardSelected += OnCardSelected;
            Context.InputProvider.CardDragStarted -= OnCardDragStarted;
            Context.InputProvider.CardDragMoved -= OnCardDragMoved;
            Context.InputProvider.CardDragEnded -= OnCardDragEnded;
            Context.InputProvider.CardDragStarted += OnCardDragStarted;
            Context.InputProvider.CardDragMoved += OnCardDragMoved;
            Context.InputProvider.CardDragEnded += OnCardDragEnded;
            Context.HeroTargetRequested -= OnHeroTargetRequested;
            Context.HeroTargetRequested += OnHeroTargetRequested;
        }

        public override void Exit()
        {
            Context.InputProvider.CardSelected -= OnCardSelected;
            Context.InputProvider.CardDragStarted -= OnCardDragStarted;
            Context.InputProvider.CardDragMoved -= OnCardDragMoved;
            Context.InputProvider.CardDragEnded -= OnCardDragEnded;
            Context.HeroTargetRequested -= OnHeroTargetRequested;
            Context.InputProvider.SetEnabled(false);
            Context.SetEnemyHeroTargetEnabled(false);
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
            UpdateHeroTargetUi(attacker);
        }

        private void UpdateHeroTargetUi(CardModel attacker)
        {
            var canTargetHero = attacker != null && Context.CanTargetEnemyHero(attacker);
            Context.SetEnemyHeroTargetEnabled(canTargetHero);
        }

        private void OnHeroTargetRequested()
        {
            if (!TryGetSelectedAttacker(out var attacker)
                || !TryBeginHeroAttack(attacker))
            {
                return;
            }

            Context.ChangeState(new BattleState(Context, true));
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
            var isValidCardHover = targetingPolicy.IsValidHover(attacker, hoverTarget);
            var isValidHeroHover = IsValidHeroDragHover(attacker, pointerPosition);

            if (isValidCardHover)
            {
                var hoverView = Context.FindView(hoverTargetId);
                Context.DragTargetingPresenter?.UpdateDrag(
                    pointerPosition,
                    hoverView != null ? hoverView.ViewTransform : null,
                    true);
                return;
            }

            Context.DragTargetingPresenter?.UpdateDrag(
                pointerPosition,
                null,
                isValidHeroHover);
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
                return;
            }

            if (TryBeginHeroAttackFromDrag(attacker, pointerPosition))
            {
                Context.ChangeState(new BattleState(Context, true));
            }
        }

        private bool IsValidHeroDragHover(CardModel attacker, Vector2 pointerPosition)
        {
            return attacker != null
                && Context.CanTargetEnemyHero(attacker)
                && Context.IsPointerOverEnemyHero(pointerPosition);
        }

        private bool TryBeginHeroAttack(CardModel attacker)
        {
            if (attacker == null || !Context.CanTargetEnemyHero(attacker))
            {
                return false;
            }

            Context.PendingAction = new BattleActionRequest(attacker, Context.HeroArena.EnemyHero);
            return true;
        }

        private bool TryBeginHeroAttackFromDrag(CardModel attacker, Vector2 pointerPosition)
        {
            if (!Context.IsPointerOverEnemyHero(pointerPosition))
            {
                return false;
            }

            return TryBeginHeroAttack(attacker);
        }
    }
}
