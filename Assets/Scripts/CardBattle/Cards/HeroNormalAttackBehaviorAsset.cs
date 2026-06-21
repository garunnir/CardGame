using Sirenix.OdinInspector;
using UnityEngine;

namespace CardGame.CardBattle.Cards
{
    [CreateAssetMenu(fileName = "HeroNormalAttack", menuName = "CardGame/CardBattle/Hero Behaviors/Normal Attack")]
    public sealed class HeroNormalAttackBehaviorAsset : HeroBehaviorAsset
    {
        [TabGroup("Tabs", "전투")]
        [LabelText("반격 피해 (카드→영웅 시)")]
        [Tooltip("0이면 HeroDataAsset.baseAttack 사용")]
        [Min(0)]
        public int counterDamageOverride;

        [TabGroup("Tabs", "연출", Icon = SdfIconType.Stars)]
        [HideLabel]
        [InlineProperty]
        public HeroNormalAttackPresentation presentation = new HeroNormalAttackPresentation();
    }
}
