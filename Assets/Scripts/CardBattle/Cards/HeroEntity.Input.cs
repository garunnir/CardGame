using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CardGame.CardBattle.Cards
{
    public sealed partial class HeroEntity
    {
        private const float LongPressDurationSeconds = 1f;
        private const float LongPressMoveCancelThreshold = 10f;

        private CancellationTokenSource longPressCts;
        private Vector2 longPressPointerDownPosition;
        private bool longPressInspectActive;
        private bool suppressNextClick;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            CancelLongPressTimer();
            longPressPointerDownPosition = eventData.position;
            longPressCts = new CancellationTokenSource();
            WaitForLongPressAsync(longPressCts.Token).Forget();
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (longPressCts == null || longPressInspectActive)
            {
                return;
            }

            var delta = eventData.position - longPressPointerDownPosition;
            var thresholdSq = LongPressMoveCancelThreshold * LongPressMoveCancelThreshold;
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

            if (longPressInspectActive)
            {
                longPressInspectActive = false;
                suppressNextClick = true;
                LongPressReleased?.Invoke(this);
            }

            CancelLongPressTimer();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (longPressInspectActive)
            {
                return;
            }

            CancelLongPressTimer();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (suppressNextClick)
            {
                suppressNextClick = false;
                return;
            }

            if (!CanAcceptShortClick)
            {
                return;
            }

            ShortClicked?.Invoke(this);
        }

        private async UniTaskVoid WaitForLongPressAsync(CancellationToken token)
        {
            try
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(LongPressDurationSeconds),
                    ignoreTimeScale: true,
                    cancellationToken: token);

                if (token.IsCancellationRequested || this == null || !viewState.IsValid)
                {
                    return;
                }

                longPressInspectActive = true;
                LongPressed?.Invoke(this);
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

        private void CancelLongPressOnDestroy()
        {
            CancelLongPressTimer();
            longPressInspectActive = false;
        }
    }
}
