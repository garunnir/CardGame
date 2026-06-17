using System;
using CardGame.CardBattle.Cards;
using UnityEngine;

namespace CardGame.CardBattle.Core
{
    /// <summary>CardView 클릭을 GameManager로 전달하는 입력 래퍼.</summary>
    public sealed class UnityInputProvider : IInputProvider
    {
        public event Action<CardModel> CardSelected;

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
            }
        }

        private void OnViewClicked(CardView view)
        {
            if (view?.BoundModel != null)
            {
                NotifyCardSelected(view.BoundModel);
            }
        }
    }
}
