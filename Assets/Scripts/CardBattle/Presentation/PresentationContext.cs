using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Core;
using CardGame.CardBattle.UI;

namespace CardGame.CardBattle.Presentation
{
    /// <summary>레거시 PresentationContext → BattlePresentationSpec 변환.</summary>
    public sealed class PresentationContext
    {
        public PresentationContext(
            BattleActionRequest request,
            AttackOutcome outcome,
            AttackPresentationSnapshot snapshot,
            BattleActionResult actionResult,
            ICardViewRegistry viewRegistry,
            UIManager ui,
            CardPresentationService presentation)
        {
            Request = request;
            Outcome = outcome;
            Snapshot = snapshot;
            ActionResult = actionResult;
            ViewRegistry = viewRegistry;
            Ui = ui;
            Presentation = presentation;
        }

        public BattleActionRequest Request { get; }
        public AttackOutcome Outcome { get; }
        public AttackPresentationSnapshot Snapshot { get; }
        public BattleActionResult ActionResult { get; }
        public AttackResolution Resolution => Outcome.Resolution;
        public CardModel Attacker => Request.Attacker;
        public CardModel Target => Request.Target;
        public CardBehaviorAsset Behavior => Attacker.Behavior;
        public int BeforeAttackerHp => Outcome.BeforeAttackerHp;
        public int BeforeTargetHp => Outcome.BeforeTargetHp;
        public ICardViewRegistry ViewRegistry { get; }
        public UIManager Ui { get; }
        public CardPresentationService Presentation { get; }

        public ICardBattleView GetView(CardInstanceId id)
        {
            return ViewRegistry != null && ViewRegistry.TryGetView(id, out var view) ? view : null;
        }

        public CardModel GetModel(CardInstanceId id)
        {
            return ViewRegistry != null && ViewRegistry.TryGetModel(id, out var model) ? model : null;
        }

        public static BattlePresentationSpec ToSpec(PresentationContext context, float tailDelay)
        {
            var unifiedSnapshot = PresentationSnapshot.FromCardAttack(context.Outcome, context.Request);
            var spec = new BattlePresentationSpec(
                PresentationKind.CardVsCard,
                unifiedSnapshot,
                context.ViewRegistry,
                null,
                context.Ui,
                context.Presentation,
                tailDelay)
            {
                AttackerCard = context.Attacker,
                PrimaryTargetCard = context.Target,
                CardBehavior = context.Behavior,
                CardOutcome = context.Outcome,
                ActionResult = context.ActionResult,
            };
            return spec;
        }
    }
}
