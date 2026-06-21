using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CardGame.CardBattle.Input
{
    /// <summary>1초 롱프레스 + 10px 이동 취소 — 카드·영웅 패널 공통.</summary>
    public sealed class LongPressPointerHandler :
        MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerMoveHandler,
        IPointerExitHandler
    {
        public const float DefaultDurationSeconds = 1f;
        public const float DefaultMoveCancelThreshold = 10f;

        [SerializeField] private float longPressDurationSeconds = DefaultDurationSeconds;
        [SerializeField] private float moveCancelThreshold = DefaultMoveCancelThreshold;

        private CancellationTokenSource longPressCts;
        private Vector2 pointerDownPosition;
        private bool longPressActive;
        private bool suppressNextClick;

        public event Action LongPressed;
        public event Action LongPressReleased;
        public event Action ShortClicked;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            CancelLongPressTimer();
            pointerDownPosition = eventData.position;
            longPressCts = new CancellationTokenSource();
            WaitForLongPressAsync(longPressCts.Token).Forget();
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (longPressCts == null || longPressActive)
            {
                return;
            }

            var delta = eventData.position - pointerDownPosition;
            var thresholdSq = moveCancelThreshold * moveCancelThreshold;
            if (delta.sqrMagnitude > thresholdSq)
            {
                CancelLongPressTimer();
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            if (longPressActive)
            {
                longPressActive = false;
                suppressNextClick = true;
                LongPressReleased?.Invoke();
            }
            else if (longPressCts != null)
            {
                suppressNextClick = true;
                ShortClicked?.Invoke();
            }

            CancelLongPressTimer();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (longPressActive)
            {
                return;
            }

            CancelLongPressTimer();
        }

        private void OnDestroy()
        {
            CancelLongPressTimer();
            longPressActive = false;
        }

        public bool ConsumeSuppressClick()
        {
            if (!suppressNextClick)
            {
                return false;
            }

            suppressNextClick = false;
            return true;
        }

        private async UniTaskVoid WaitForLongPressAsync(CancellationToken token)
        {
            try
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(longPressDurationSeconds),
                    ignoreTimeScale: true,
                    cancellationToken: token);

                if (token.IsCancellationRequested || this == null)
                {
                    return;
                }

                longPressActive = true;
                LongPressed?.Invoke();
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void CancelLongPressTimer()
        {
            if (longPressCts == null)
            {
                return;
            }

            longPressCts.Cancel();
            longPressCts.Dispose();
            longPressCts = null;
        }
    }
}
