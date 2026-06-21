using CardGame.CardBattle.Cards;

namespace CardGame.CardBattle.Presentation
{
    /// <summary>Build phase — behavior SO에서 ProjectilePresentationAsset resolve.</summary>
    public static class PresentationAssetResolve
    {
        public static ProjectilePresentationAsset ResolveAttack(CardBehaviorAsset behavior)
        {
            return behavior is RangedBehaviorAsset ranged
                ? ranged.attackProjectilePresentation
                : null;
        }

        public static ProjectilePresentationAsset ResolveTurnHeal(CardBehaviorAsset behavior)
        {
            return behavior is HealerBehaviorAsset healer
                ? healer.turnHealProjectilePresentation
                : null;
        }
    }
}
