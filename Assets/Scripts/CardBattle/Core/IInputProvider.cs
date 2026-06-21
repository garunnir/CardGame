using System;
using UnityEngine;

namespace CardGame.CardBattle.Core
{
    /// <summary>터치/클릭 입력 추상화 — 카드·영웅 패널.</summary>
    public interface IInputProvider
    {
        event Action<CardInstanceId> CardSelected;
        event Action<CardInstanceId> CardLongPressed;
        event Action<CardInstanceId> CardLongPressReleased;
        event Action<CardInstanceId, Vector2> CardDragStarted;
        event Action<CardInstanceId, CardInstanceId, Vector2> CardDragMoved;
        event Action<CardInstanceId, CardInstanceId, Vector2> CardDragEnded;
        event Action<HeroInstanceId> HeroLongPressed;
        event Action<HeroInstanceId> HeroLongPressReleased;
        bool IsEnabled { get; }

        void SetEnabled(bool enabled);
        void NotifyCardSelected(CardInstanceId cardId);
        void NotifyCardDragStarted(CardInstanceId sourceId, Vector2 pointerPosition);
        void NotifyCardDragMoved(CardInstanceId sourceId, CardInstanceId hoverTargetId, Vector2 pointerPosition);
        void NotifyCardDragEnded(CardInstanceId sourceId, CardInstanceId dropTargetId, Vector2 pointerPosition);
    }
}
