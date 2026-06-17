using System.Collections.Generic;
using CardGame.CardBattle.Cards;
using UnityEngine;

namespace CardGame.CardBattle.Bridge
{
    /// <summary>Resources 폴더 기반 폴백 로더. Addressables 구현체로 교체 가능.</summary>
    public sealed class ResourcesCardDataLoader : ICardDataLoader
    {
        private readonly string resourceFolder;

        public ResourcesCardDataLoader(string resourceFolder = "CardBattle/Cards")
        {
            this.resourceFolder = resourceFolder;
        }

        public CardDataAsset LoadById(string cardId)
        {
            var all = Resources.LoadAll<CardDataAsset>(resourceFolder);
            for (var i = 0; i < all.Length; i++)
            {
                if (all[i].cardId == cardId)
                {
                    return all[i];
                }
            }

            Debug.LogWarning($"CardDataAsset not found: {cardId}");
            return null;
        }

        public List<CardDataAsset> LoadDeck(IReadOnlyList<string> cardIds)
        {
            var list = new List<CardDataAsset>();
            for (var i = 0; i < cardIds.Count; i++)
            {
                var asset = LoadById(cardIds[i]);
                if (asset != null)
                {
                    list.Add(asset);
                }
            }

            return list;
        }
    }
}
