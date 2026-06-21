using System;
using System.Collections.Generic;
using CardGame.CardBattle.Cards;
using UnityEngine;

namespace CardGame.CardBattle.Core
{
    /// <summary>카드·영웅 패널 클릭 입력 추상화.</summary>
    public sealed class UnityInputProvider : IInputProvider
    {
        private readonly List<ICardInputHost> boundHosts = new List<ICardInputHost>();
        private readonly HashSet<ICardInputHost> boundHostSet = new HashSet<ICardInputHost>();
        private IHeroInputHost boundPlayerHeroHost;
        private IHeroInputHost boundEnemyHeroHost;

        public event Action<CardInstanceId> CardSelected;
        public event Action<CardInstanceId> CardLongPressed;
        public event Action<CardInstanceId> CardLongPressReleased;
        public event Action<CardInstanceId, Vector2> CardDragStarted;
        public event Action<CardInstanceId, CardInstanceId, Vector2> CardDragMoved;
        public event Action<CardInstanceId, CardInstanceId, Vector2> CardDragEnded;
        public event Action<HeroInstanceId> HeroLongPressed;
        public event Action<HeroInstanceId> HeroLongPressReleased;

        public bool IsEnabled { get; private set; }

        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
        }

        public void NotifyCardSelected(CardInstanceId cardId)
        {
            if (!IsEnabled || !cardId.IsValid)
            {
                return;
            }

            CardSelected?.Invoke(cardId);
        }

        public void NotifyCardLongPressed(CardInstanceId cardId)
        {
            if (!cardId.IsValid)
            {
                return;
            }

            CardLongPressed?.Invoke(cardId);
        }

        public void NotifyCardLongPressReleased(CardInstanceId cardId)
        {
            if (!cardId.IsValid)
            {
                return;
            }

            CardLongPressReleased?.Invoke(cardId);
        }

        public void NotifyCardDragStarted(CardInstanceId sourceId, Vector2 pointerPosition)
        {
            if (!IsEnabled || !sourceId.IsValid)
            {
                return;
            }

            CardDragStarted?.Invoke(sourceId, pointerPosition);
        }

        public void NotifyCardDragMoved(CardInstanceId sourceId, CardInstanceId hoverTargetId, Vector2 pointerPosition)
        {
            if (!IsEnabled || !sourceId.IsValid)
            {
                return;
            }

            CardDragMoved?.Invoke(sourceId, hoverTargetId, pointerPosition);
        }

        public void NotifyCardDragEnded(CardInstanceId sourceId, CardInstanceId dropTargetId, Vector2 pointerPosition)
        {
            if (!IsEnabled || !sourceId.IsValid)
            {
                return;
            }

            CardDragEnded?.Invoke(sourceId, dropTargetId, pointerPosition);
        }

        public void BindInputHosts(IReadOnlyList<ICardInputHost> hosts)
        {
            for (var i = 0; i < boundHosts.Count; i++)
            {
                UnbindHost(boundHosts[i]);
            }

            boundHosts.Clear();
            boundHostSet.Clear();

            if (hosts == null)
            {
                return;
            }

            for (var i = 0; i < hosts.Count; i++)
            {
                var host = hosts[i];
                if (host == null || !boundHostSet.Add(host))
                {
                    continue;
                }

                host.Clicked += OnHostClicked;
                host.LongPressed += OnHostLongPressed;
                host.LongPressReleased += OnHostLongPressReleased;
                host.DragStarted += OnHostDragStarted;
                host.DragMoved += OnHostDragMoved;
                host.DragEnded += OnHostDragEnded;
                boundHosts.Add(host);
            }
        }

        public void BindHeroInputHosts(IHeroInputHost playerHeroHost, IHeroInputHost enemyHeroHost)
        {
            UnbindHeroHost(boundPlayerHeroHost);
            UnbindHeroHost(boundEnemyHeroHost);

            boundPlayerHeroHost = playerHeroHost;
            boundEnemyHeroHost = enemyHeroHost;

            BindHeroHost(boundPlayerHeroHost);
            BindHeroHost(boundEnemyHeroHost);
        }

        private void BindHeroHost(IHeroInputHost host)
        {
            if (host == null)
            {
                return;
            }

            host.LongPressed += OnHeroHostLongPressed;
            host.LongPressReleased += OnHeroHostLongPressReleased;
            host.ShortClicked += OnHeroHostShortClicked;
        }

        private void UnbindHeroHost(IHeroInputHost host)
        {
            if (host == null)
            {
                return;
            }

            host.LongPressed -= OnHeroHostLongPressed;
            host.LongPressReleased -= OnHeroHostLongPressReleased;
            host.ShortClicked -= OnHeroHostShortClicked;
        }

        private void UnbindHost(ICardInputHost host)
        {
            if (host == null)
            {
                return;
            }

            host.Clicked -= OnHostClicked;
            host.LongPressed -= OnHostLongPressed;
            host.LongPressReleased -= OnHostLongPressReleased;
            host.DragStarted -= OnHostDragStarted;
            host.DragMoved -= OnHostDragMoved;
            host.DragEnded -= OnHostDragEnded;
        }

        private void OnHostClicked(ICardInputHost host)
        {
            if (host != null && host.InstanceId.IsValid)
            {
                NotifyCardSelected(host.InstanceId);
            }
        }

        private void OnHostLongPressed(ICardInputHost host)
        {
            if (host != null && host.InstanceId.IsValid)
            {
                NotifyCardLongPressed(host.InstanceId);
            }
        }

        private void OnHostLongPressReleased(ICardInputHost host)
        {
            if (host != null && host.InstanceId.IsValid)
            {
                NotifyCardLongPressReleased(host.InstanceId);
            }
        }

        private void OnHostDragStarted(ICardInputHost host, Vector2 pointerPosition)
        {
            if (host != null && host.InstanceId.IsValid)
            {
                NotifyCardDragStarted(host.InstanceId, pointerPosition);
            }
        }

        private void OnHostDragMoved(ICardInputHost source, ICardInputHost hoveredHost, Vector2 pointerPosition)
        {
            if (source == null || !source.InstanceId.IsValid)
            {
                return;
            }

            var hoverId = hoveredHost != null ? hoveredHost.InstanceId : default;
            NotifyCardDragMoved(source.InstanceId, hoverId, pointerPosition);
        }

        private void OnHostDragEnded(ICardInputHost source, ICardInputHost dropHost, Vector2 pointerPosition)
        {
            if (source == null || !source.InstanceId.IsValid)
            {
                return;
            }

            var dropId = dropHost != null ? dropHost.InstanceId : default;
            NotifyCardDragEnded(source.InstanceId, dropId, pointerPosition);
        }

        private void OnHeroHostLongPressed(IHeroInputHost host)
        {
            if (host != null && host.InstanceId.IsValid)
            {
                HeroLongPressed?.Invoke(host.InstanceId);
            }
        }

        private void OnHeroHostLongPressReleased(IHeroInputHost host)
        {
            if (host != null && host.InstanceId.IsValid)
            {
                HeroLongPressReleased?.Invoke(host.InstanceId);
            }
        }

        private void OnHeroHostShortClicked(IHeroInputHost host)
        {
            // short click 타겟은 HeroArenaPresenter.ShortClickedDirect → GameManager.HeroTargetRequested
        }
    }
}
