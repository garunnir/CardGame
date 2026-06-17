using System;
using CardGame.CardBattle.Cards;
using UnityEngine;

namespace CardGame.CardBattle.Core
{
    /// <summary>터치/클릭 입력 추상화.</summary>
    public interface IInputProvider
    {
        event Action<CardModel> CardSelected;
        event Action<CardModel, Vector2> CardDragStarted;
        event Action<CardModel, CardModel, Vector2> CardDragMoved;
        event Action<CardModel, CardModel, Vector2> CardDragEnded;
        bool IsEnabled { get; }

        void SetEnabled(bool enabled);
        void NotifyCardSelected(CardModel card);
        void NotifyCardDragStarted(CardModel source, Vector2 pointerPosition);
        void NotifyCardDragMoved(CardModel source, CardModel hoverTarget, Vector2 pointerPosition);
        void NotifyCardDragEnded(CardModel source, CardModel dropTarget, Vector2 pointerPosition);
    }
}
