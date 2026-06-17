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
    }
}
