using Sirenix.OdinInspector;
using UnityEngine;

namespace CardGame.CardBattle.Cards
{
    [CreateAssetMenu(fileName = "HeroShield", menuName = "CardGame/CardBattle/Hero Behaviors/Shield")]
    public sealed class HeroShieldBehaviorAsset : HeroBehaviorAsset
    {
        [TabGroup("Tabs", "전투")]
        [LabelText("보호막량")]
        [Min(1)]
        public int shieldAmount = 5;

        [TabGroup("Tabs", "연출", Icon = SdfIconType.Stars)]
        [HideLabel]
        [InlineProperty]
        public HeroShieldPresentation presentation = new HeroShieldPresentation();
    }
}
