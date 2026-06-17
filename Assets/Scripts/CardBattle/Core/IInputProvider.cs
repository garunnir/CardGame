using System;
using CardGame.CardBattle.Cards;

namespace CardGame.CardBattle.Core
{
    /// <summary>터치/클릭 입력 추상화.</summary>
    public interface IInputProvider
    {
        event Action<CardModel> CardSelected;
        bool IsEnabled { get; }

        void SetEnabled(bool enabled);
        void NotifyCardSelected(CardModel card);
    }
}
