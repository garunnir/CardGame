using Sirenix.OdinInspector;
using UnityEngine;

using CardGame.CardBattle.Presentation;

namespace CardGame.CardBattle.Cards
{
    [CreateAssetMenu(fileName = "HealerBehavior", menuName = "CardGame/CardBattle/Behaviors/Healer")]
    public class HealerBehaviorAsset : CardBehaviorAsset
    {
        [TabGroup("Tabs", "전투", Icon = SdfIconType.Lightning)]
        [LabelText("공격 피해 배율")]
        [Min(0f)]
        public float primaryDamageMultiplier = 1f;

        [TabGroup("Tabs", "전투")]
        [LabelText("반격 받음")]
        public bool receivesCounterAttack = true;

        [TabGroup("Tabs", "전투")]
        [LabelText("턴 시작 회복량")]
        [Min(0)]
        public int turnHealAmount = 1;

        [TabGroup("Tabs", "전투")]
        [LabelText("자신 제외")]
        public bool excludesSelf = true;

        [TabGroup("Tabs", "연출", Icon = SdfIconType.Stars)]
        [HideLabel]
        [InlineProperty]
        public HealerBehaviorPresentation presentation = new HealerBehaviorPresentation();

        public override CardType StrategyType => CardType.Healer;

        public int ScalePrimaryDamage(int attackPower)
        {
            return BehaviorDamageMath.ScalePrimary(primaryDamageMultiplier, attackPower);
        }

        public override void CollectAttackModules(IAttackModuleCollector collector)
        {
            collector.AddPrimary(new PrimaryDamageModule(ap => ScalePrimaryDamage(ap.AttackPower)));
            collector.AddCounter(new CounterAttackModule(receivesCounterAttack));
        }

        public override void CollectPresentationModules(IPresentationModuleCollector collector)
        {
            collector.Add(new MeleeAttackPresentationModule(
                presentation.attackSfx,
                presentation.attackVfxPrefab,
                presentation.hitSfx,
                presentation.hitVfxPrefab,
                0f,
                0f));
            collector.Add(new CounterPresentationModule());
            collector.Add(new DefaultCameraShakePresentationModule());
        }
    }
}
