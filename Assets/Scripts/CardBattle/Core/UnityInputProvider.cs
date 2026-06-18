using System;
using CardGame.CardBattle.Cards;
using UnityEngine;

namespace CardGame.CardBattle.Core
{
    /// <summary>카드 클릭·드래그 입력 추상화.</summary>
    public sealed class UnityInputProvider : IInputProvider
    {
        private readonly System.Collections.Generic.List<ICardInputHost> boundHosts =
            new System.Collections.Generic.List<ICardInputHost>();

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

        public void BindInputHosts(System.Collections.Generic.IReadOnlyList<ICardInputHost> hosts)
        {
            for (var i = 0; i < boundHosts.Count; i++)
            {
                UnbindHost(boundHosts[i]);
            }

            boundHosts.Clear();

            if (hosts == null)
            {
                return;
            }

            for (var i = 0; i < hosts.Count; i++)
            {
                var host = hosts[i];
                if (host == null || boundHosts.Contains(host))
                {
                    continue;
                }

                host.Clicked += OnHostClicked;
                host.DragStarted += OnHostDragStarted;
                host.DragMoved += OnHostDragMoved;
                host.DragEnded += OnHostDragEnded;
                boundHosts.Add(host);
            }
        }

        private void UnbindHost(ICardInputHost host)
        {
            if (host == null)
            {
                return;
            }

            host.Clicked -= OnHostClicked;
            host.DragStarted -= OnHostDragStarted;
            host.DragMoved -= OnHostDragMoved;
            host.DragEnded -= OnHostDragEnded;
        }

        private void OnHostClicked(ICardInputHost host)
        {
            if (host?.BoundModel != null)
            {
                NotifyCardSelected(host.BoundModel);
            }
        }

        private void OnHostDragStarted(ICardInputHost host, Vector2 pointerPosition)
        {
            NotifyCardDragStarted(host?.BoundModel, pointerPosition);
        }

        private void OnHostDragMoved(ICardInputHost source, ICardInputHost hoveredHost, Vector2 pointerPosition)
        {
            NotifyCardDragMoved(
                source?.BoundModel,
                hoveredHost?.BoundModel,
                pointerPosition);
        }

        private void OnHostDragEnded(ICardInputHost source, ICardInputHost dropHost, Vector2 pointerPosition)
        {
            NotifyCardDragEnded(
                source?.BoundModel,
                dropHost?.BoundModel,
                pointerPosition);
        }
    }
}
