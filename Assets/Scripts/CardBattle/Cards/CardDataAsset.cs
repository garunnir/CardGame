using Sirenix.OdinInspector;
using UnityEngine;

namespace CardGame.CardBattle.Cards
{
    /// <summary>기획 테이블 연동용 카드 스탯 ScriptableObject.</summary>
    [CreateAssetMenu(fileName = "CardData", menuName = "CardGame/CardBattle/Card Data")]
    public class CardDataAsset : ScriptableObject
    {
        [BoxGroup("기본 정보", centerLabel: true)]
        [LabelText("카드 ID")]
        public string cardId;

        [BoxGroup("기본 정보")]
        [LabelText("표시 이름")]
        public string displayName;

        [BoxGroup("기본 정보")]
        [LabelText("최대 HP")]
        [Min(1)]
        public int maxHp = 5;

        [BoxGroup("기본 정보")]
        [LabelText("일러스트")]
        [PreviewField(Height = 80)]
        public Sprite illustration;

        [BoxGroup("행동", centerLabel: true)]
        [LabelText("행동 SO")]
        [AssetSelector]
        public CardBehaviorAsset behavior;

        [BoxGroup("상세 보기 (선택)", centerLabel: true)]
        [LabelText("타입 표시 덮어쓰기")]
        [Tooltip("비어 있으면 Behavior SO → 코드 기본값 순.")]
        public string detailTypeLabelOverride;

        [BoxGroup("상세 보기 (선택)")]
        [LabelText("설명 덮어쓰기")]
        [Tooltip("비어 있으면 Behavior SO → 코드 기본값 순.")]
        [TextArea(2, 6)]
        public string detailDescriptionOverride;

        [BoxGroup("영웅 지원", centerLabel: true)]
        public HeroSupportDefinition heroSupport;
    }
}
