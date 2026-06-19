using System.Collections.Generic;
using CardGame.CardBattle.Input;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CardGame.CardBattle.Cards
{
    public sealed partial class CardEntity
    {
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
