using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CardGame.CardBattle.Cards
{
    public sealed partial class CardEntity
    {
        private const float LongPressDurationSeconds = 1f;
        private const float LongPressMoveCancelThreshold = 10f;

        // 이동 취소: UnityEngine.Input 폴링 금지 — IPointerMoveHandler + PointerEventData (Input System UI).

        private CancellationTokenSource longPressCts;
        private Vector2 longPressPointerDownPosition;
        private bool longPressInspectActive;

        public void SetHoverState(bool isActive, bool isValid)
        {
            if (isActive)
            {
                var tint = isValid ? hoverValidColor : hoverInvalidColor;
                if (frontFace != null)
                {
                    frontFace.SetColor(tint);
                }

                if (backFace != null)
                {
                    backFace.SetColor(tint);
                }

                return;
            }

            if (frontFace != null)
            {
                frontFace.SetColor(frontBaseColor);
            }

            if (backFace != null)
            {
                backFace.SetColor(backBaseColor);
            }
        }

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

            Clicked?.Invoke(this);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            CancelLongPressTimer();

            if (eventData.button != PointerEventData.InputButton.Left || !CanBeginDrag)
            {
                dragStarted = false;
                return;
            }

            dragStarted = true;
            DragStarted?.Invoke(this, eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragStarted)
            {
                return;
            }

            DragMoved?.Invoke(this, FindHoveredHost(eventData), eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!dragStarted)
            {
                return;
            }

            dragStarted = false;
            suppressNextClick = true;
            DragEnded?.Invoke(this, FindHoveredHost(eventData), eventData.position);
        }

        private async UniTaskVoid WaitForLongPressAsync(CancellationToken token)
        {
            try
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(LongPressDurationSeconds),
                    ignoreTimeScale: true,
                    cancellationToken: token);

                if (token.IsCancellationRequested || this == null)
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

        private ICardInputHost FindHoveredHost(PointerEventData eventData)
        {
            if (EventSystem.current == null || eventData == null)
            {
                return null;
            }

            PointerRaycastResults.Clear();
            EventSystem.current.RaycastAll(eventData, PointerRaycastResults);
            for (var i = 0; i < PointerRaycastResults.Count; i++)
            {
                var hitObject = PointerRaycastResults[i].gameObject;
                if (hitObject == null)
                {
                    continue;
                }

                if (hitObject.transform.IsChildOf(transform))
                {
                    continue;
                }

                var candidate = hitObject.GetComponentInParent<CardEntity>();
                if (candidate != null && candidate != this && candidate.CanAcceptTarget)
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
