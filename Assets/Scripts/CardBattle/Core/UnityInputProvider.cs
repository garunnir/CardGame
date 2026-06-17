using System;
using CardGame.CardBattle.Cards;
using UnityEngine;

namespace CardGame.CardBattle.Core
{
    /// <summary>CardView 클릭을 GameManager로 전달하는 입력 래퍼.</summary>
    public sealed class UnityInputProvider : IInputProvider
    {
        public event Action<CardModel> CardSelected;
        public event Action<CardModel, Vector2> CardDragStarted;
        public event Action<CardModel, CardModel, Vector2> CardDragMoved;
        public event Action<CardModel, CardModel, Vector2> CardDragEnded;

        public bool IsEnabled { get; private set; }

        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
        }

        public void NotifyCardSelected(CardModel card)
        {
            if (!IsEnabled || card == null)
            {
                return;
            }

            CardSelected?.Invoke(card);
        }

        public void NotifyCardDragStarted(CardModel source, Vector2 pointerPosition)
        {
            if (!IsEnabled || source == null)
            {
                return;
            }

            CardDragStarted?.Invoke(source, pointerPosition);
        }

        public void NotifyCardDragMoved(CardModel source, CardModel hoverTarget, Vector2 pointerPosition)
        {
            if (!IsEnabled || source == null)
            {
                return;
            }

            CardDragMoved?.Invoke(source, hoverTarget, pointerPosition);
        }

        public void NotifyCardDragEnded(CardModel source, CardModel dropTarget, Vector2 pointerPosition)
        {
            if (!IsEnabled || source == null)
            {
                return;
            }

            CardDragEnded?.Invoke(source, dropTarget, pointerPosition);
        }

        public void BindViews(CardView[] views)
        {
            if (views == null)
            {
                return;
            }

            for (var i = 0; i < views.Length; i++)
            {
                var view = views[i];
                if (view == null)
                {
                    continue;
                }

                view.Clicked -= OnViewClicked;
                view.Clicked += OnViewClicked;
                view.DragStarted -= OnViewDragStarted;
                view.DragMoved -= OnViewDragMoved;
                view.DragEnded -= OnViewDragEnded;
                view.DragStarted += OnViewDragStarted;
                view.DragMoved += OnViewDragMoved;
                view.DragEnded += OnViewDragEnded;
            }
        }

        private void OnViewClicked(CardView view)
        {
            if (view?.BoundModel != null)
            {
                NotifyCardSelected(view.BoundModel);
            }
        }

        private void OnViewDragStarted(CardView view, Vector2 pointerPosition)
        {
            NotifyCardDragStarted(view?.BoundModel, pointerPosition);
        }

        private void OnViewDragMoved(CardView view, CardView hoveredView, Vector2 pointerPosition)
        {
            NotifyCardDragMoved(
                view?.BoundModel,
                hoveredView != null ? hoveredView.BoundModel : null,
                pointerPosition);
        }

        private void OnViewDragEnded(CardView view, CardView hoveredView, Vector2 pointerPosition)
        {
            NotifyCardDragEnded(
                view?.BoundModel,
                hoveredView != null ? hoveredView.BoundModel : null,
                pointerPosition);
        }
    }
}
