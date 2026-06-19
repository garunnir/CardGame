using Sirenix.OdinInspector;
using UnityEngine;

namespace CardGame.CardBattle.Cards
{
    [CreateAssetMenu(fileName = "NormalBehavior", menuName = "CardGame/CardBattle/Behaviors/Normal")]
    public class NormalBehaviorAsset : CardBehaviorAsset
    {
        [TabGroup("Tabs", "전투", Icon = SdfIconType.Lightning)]
        [LabelText("피해 배율")]
        [Tooltip("1.0 = 현재 HP 100%")]
        [Min(0f)]
        public float primaryDamageMultiplier = 1f;

        [TabGroup("Tabs", "전투")]
        [LabelText("반격 받음")]
        public bool receivesCounterAttack = true;

        [TabGroup("Tabs", "연출", Icon = SdfIconType.Stars)]
        [HideLabel]
        [InlineProperty]
        public NormalBehaviorPresentation presentation = new NormalBehaviorPresentation();

        public override CardType StrategyType => CardType.Normal;

        public int ScalePrimaryDamage(int attackPower)
        {
            return BehaviorDamageMath.ScalePrimary(primaryDamageMultiplier, attackPower);
        }

        public override void CollectAttackModules(IAttackModuleCollector collector)
        {
            collector.AddPrimary(new PrimaryDamageModule(ap => ScalePrimaryDamage(ap.AttackPower)));
            collector.AddCounter(new CounterAttackModule(receivesCounterAttack));
        }
    }
}
