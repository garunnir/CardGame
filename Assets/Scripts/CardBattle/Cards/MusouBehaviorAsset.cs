using Sirenix.OdinInspector;
using UnityEngine;

using CardGame.CardBattle.Presentation;

namespace CardGame.CardBattle.Cards
{
    [CreateAssetMenu(fileName = "MusouBehavior", menuName = "CardGame/CardBattle/Behaviors/Musou")]
    public class MusouBehaviorAsset : CardBehaviorAsset
    {
        [TabGroup("Tabs", "전투", Icon = SdfIconType.Lightning)]
        [LabelText("1타 피해 배율")]
        [Min(0f)]
        public float primaryDamageMultiplier = 1f;

        [TabGroup("Tabs", "전투")]
        [LabelText("반격 받음")]
        public bool receivesCounterAttack = true;

        [TabGroup("Tabs", "전투")]
        [LabelText("2타 피해 비율")]
        [Range(0f, 2f)]
        public float secondaryDamageRatio = 0.5f;

        [TabGroup("Tabs", "연출", Icon = SdfIconType.Stars)]
        [HideLabel]
        [InlineProperty]
        public MusouBehaviorPresentation presentation = new MusouBehaviorPresentation();

        public override CardType StrategyType => CardType.Musou;

        public int ScalePrimaryDamage(int attackPower)
        {
            return BehaviorDamageMath.ScalePrimary(primaryDamageMultiplier, attackPower);
        }

        public int ScaleSecondaryDamage(int attackPower)
        {
            return BehaviorDamageMath.ScaleSecondary(secondaryDamageRatio, attackPower);
        }

        public override void CollectAttackModules(IAttackModuleCollector collector)
        {
            collector.AddPrimary(new PrimaryDamageModule(ap => ScalePrimaryDamage(ap.AttackPower)));
            collector.AddCounter(new CounterAttackModule(receivesCounterAttack));
            collector.AddSecondary(new AdjacentSplashModule(this));
        }

        public override void CollectPresentationModules(IPresentationModuleCollector collector)
        {
            collector.Add(new MeleeAttackPresentationModule(
                presentation.attackSfx,
                presentation.attackVfxPrefab,
                presentation.hitSfx,
                presentation.hitVfxPrefab,
                presentation.attackDashDuration,
                0f));
            collector.Add(new CounterPresentationModule());
            collector.Add(new MusouSecondaryPresentationModule(
                presentation.secondaryHitDelay,
                presentation.secondaryCameraShake));
        }
    }
}
