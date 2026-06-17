using System.Collections.Generic;
using CardGame.CardBattle.Cards;
using UnityEngine;

namespace CardGame.CardBattle.Core
{
    /// <summary>CardDataAsset 미배치 시 런타임 테스트 덱 생성.</summary>
    public static class RuntimeDeckFactory
    {
        private static readonly CardType[] DefaultPattern =
        {
            CardType.Normal,
            CardType.Ranged,
            CardType.Musou,
            CardType.Healer,
            CardType.Normal,
            CardType.Ranged
        };

        public static List<CardDataAsset> CreateDefaultDeck(string teamPrefix)
        {
            var list = new List<CardDataAsset>(6);
            for (var i = 0; i < DefaultPattern.Length; i++)
            {
                var asset = ScriptableObject.CreateInstance<CardDataAsset>();
                asset.cardId = $"{teamPrefix}_{DefaultPattern[i]}_{i}";
                asset.displayName = $"{teamPrefix} {DefaultPattern[i]}";
                asset.cardType = DefaultPattern[i];
                asset.maxHp = DefaultPattern[i] == CardType.Musou ? 4 : 5;
                list.Add(asset);
            }

            return list;
        }

        public static bool IsDeckValid(IList<CardDataAsset> deck)
        {
            if (deck == null || deck.Count < BattleField.SlotCount * 2)
            {
                return false;
            }

            for (var i = 0; i < deck.Count; i++)
            {
                if (deck[i] == null)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
