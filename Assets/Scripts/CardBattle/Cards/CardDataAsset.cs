using UnityEngine;

namespace CardGame.CardBattle.Cards
{
    /// <summary>기획 테이블 연동용 카드 스탯 ScriptableObject.</summary>
    [CreateAssetMenu(fileName = "CardData", menuName = "CardGame/CardBattle/Card Data")]
    public class CardDataAsset : ScriptableObject
    {
        [Tooltip("카드 ID")]
        public string cardId;

        [Tooltip("표시 이름")]
        public string displayName;

        [Tooltip("카드 타입")]
        public CardType cardType = CardType.Normal;

        [Tooltip("최대 HP")]
        [Min(1)]
        public int maxHp = 5;

        [Tooltip("일러스트")]
        public Sprite illustration;
    }
}
