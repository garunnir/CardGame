using System;
using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Core;
using CardGame.CardBattle.UI;

namespace CardGame.CardBattle.Presentation
{
    public sealed class PresentationContext
    {
        public PresentationContext(
            BattleActionRequest request,
            AttackResolution resolution,
            int beforeAttackerHp,
            int beforeTargetHp,
            Func<CardModel, ICardBattleView> findView,
            UIManager ui,
            CardPresentationService presentation)
        {
            Request = request;
            Resolution = resolution;
            BeforeAttackerHp = beforeAttackerHp;
            BeforeTargetHp = beforeTargetHp;
            FindView = findView;
            Ui = ui;
            Presentation = presentation;
        }

        public BattleActionRequest Request { get; }
        public AttackResolution Resolution { get; }
        public CardModel Attacker => Request.Attacker;
        public CardModel Target => Request.Target;
        public CardBehaviorAsset Behavior => Attacker.Behavior;
        public int BeforeAttackerHp { get; }
        public int BeforeTargetHp { get; }
        public Func<CardModel, ICardBattleView> FindView { get; }
        public UIManager Ui { get; }
        public CardPresentationService Presentation { get; }

        public int AppliedPrimaryDamage { get; set; }
        public int AppliedCounterDamage { get; set; }
        public SecondaryStrikeResult AppliedSecondary { get; set; }
    }
}
