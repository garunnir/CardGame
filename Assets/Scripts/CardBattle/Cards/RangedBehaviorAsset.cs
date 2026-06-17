using Sirenix.OdinInspector;
using UnityEngine;

using CardGame.CardBattle.Presentation;

namespace CardGame.CardBattle.Cards
{
    [CreateAssetMenu(fileName = "RangedBehavior", menuName = "CardGame/CardBattle/Behaviors/Ranged")]
    public class RangedBehaviorAsset : CardBehaviorAsset
    {
        [TabGroup("Tabs", "전투", Icon = SdfIconType.Lightning)]
        [LabelText("피해 배율")]
        [Min(0f)]
        public float primaryDamageMultiplier = 1f;

        [TabGroup("Tabs", "연출", Icon = SdfIconType.Stars)]
        [HideLabel]
        [InlineProperty]
        public RangedBehaviorPresentation presentation = new RangedBehaviorPresentation();

        public override CardType StrategyType => CardType.Ranged;

        public int ScalePrimaryDamage(int attackPower)
        {
            return BehaviorDamageMath.ScalePrimary(primaryDamageMultiplier, attackPower);
        }

        public override void CollectAttackModules(IAttackModuleCollector collector)
        {
            collector.AddPrimary(new PrimaryDamageModule(ap => ScalePrimaryDamage(ap.AttackPower)));
        }

        public override void CollectPresentationModules(IPresentationModuleCollector collector)
        {
            collector.Add(new RangedAttackPresentationModule(presentation.shootDuration));
            collector.Add(new DefaultCameraShakePresentationModule());
        }
    }
}
