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

        [BoxGroup("상세 보기", centerLabel: true)]
        [LabelText("타입 표시")]
        [Tooltip("롱프레스 상세 UI. 비어 있으면 코드 기본값.")]
        public string detailTypeLabel;

        [BoxGroup("상세 보기")]
        [LabelText("설명")]
        [Tooltip("여러 줄 가능. 비어 있으면 전투 SO 수치 기반 자동 문구.")]
        [TextArea(2, 6)]
        public string detailDescription;

        [BoxGroup("상세 보기")]
        [LabelText("뒷면 이름")]
        public string hiddenDetailLabel = "???";

        [BoxGroup("상세 보기")]
        [LabelText("뒷면 설명")]
        [TextArea(1, 3)]
        public string hiddenDetailDescription = "알 수 없음";

        public abstract CardType StrategyType { get; }

        public abstract void CollectAttackModules(IAttackModuleCollector collector);
    }
}
