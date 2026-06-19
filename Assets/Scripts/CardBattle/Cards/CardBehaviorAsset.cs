using Sirenix.OdinInspector;
using UnityEngine;

namespace CardGame.CardBattle.Cards
{
    /// <summary>행동 SO 추상 베이스. 타입별 전용 SO가 상속.</summary>
    public abstract class CardBehaviorAsset : ScriptableObject
    {
        [BoxGroup("식별", centerLabel: true)]
        [LabelText("행동 ID")]
        [Tooltip("기획 테이블 연동용")]
        public string behaviorId;

        [ShowInInspector]
        [BoxGroup("식별")]
        [LabelText("전략 타입")]
        [ReadOnly]
        public CardType StrategyTypeDisplay => StrategyType;

        public abstract CardType StrategyType { get; }

        public abstract void CollectAttackModules(IAttackModuleCollector collector);
    }
}
