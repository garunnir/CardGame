using System.Collections.Generic;
using CardGame.CardBattle.Cards;

namespace CardGame.CardBattle.Bridge
{
    /// <summary>메인 CSV/JSON/DB ID → CardDataAsset 로드 (Addressables 확장 지점).</summary>
    public interface ICardDataLoader
    {
        CardDataAsset LoadById(string cardId);
        List<CardDataAsset> LoadDeck(IReadOnlyList<string> cardIds);
    }
}
