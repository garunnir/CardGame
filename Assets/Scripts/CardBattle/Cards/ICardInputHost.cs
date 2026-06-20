using System;
using CardGame.CardBattle.Core;
using UnityEngine;

namespace CardGame.CardBattle.Cards
{
    /// <summary>클릭·드래그 입력을 GameManager로 전달하는 카드 호스트 계약.</summary>
    public interface ICardInputHost
    {
        CardInstanceId InstanceId { get; }
        Transform InputTransform { get; }
        bool CanBeginDrag { get; }
        bool CanAcceptTarget { get; }

        event Action<ICardInputHost> Clicked;
        event Action<ICardInputHost> LongPressed;
        event Action<ICardInputHost> LongPressReleased;
        event Action<ICardInputHost, Vector2> DragStarted;
        event Action<ICardInputHost, ICardInputHost, Vector2> DragMoved;
        event Action<ICardInputHost, ICardInputHost, Vector2> DragEnded;
    }
}
