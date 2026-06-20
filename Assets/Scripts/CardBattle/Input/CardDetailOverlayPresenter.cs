using CardGame.CardBattle.Cards;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardGame.CardBattle.Input
{
    /// <summary>롱프레스 카드 상세 — Screen Space 오버레이.</summary>
    public sealed class CardDetailOverlayPresenter : MonoBehaviour
    {
        [SerializeField] private CanvasGroup rootGroup;
        [SerializeField] private Image cardImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI statsText;
        [SerializeField] private TextMeshProUGUI typeText;
        [SerializeField] private TextMeshProUGUI contextText;

        private void Awake()
        {
            HideImmediate();
        }

        public void Show(CardDetailContext context)
        {
            if (rootGroup == null)
            {
                return;
            }

            ApplyContext(context);
            rootGroup.gameObject.SetActive(true);
            rootGroup.alpha = 1f;
            rootGroup.blocksRaycasts = true;
        }

        public void Hide()
        {
            HideImmediate();
        }

        private void HideImmediate()
        {
            if (rootGroup == null)
            {
                return;
            }

            rootGroup.alpha = 0f;
            rootGroup.blocksRaycasts = false;
            rootGroup.gameObject.SetActive(false);
        }

        private void ApplyContext(CardDetailContext context)
        {
            if (cardImage != null)
            {
                cardImage.sprite = context.CardSprite;
                cardImage.enabled = context.CardSprite != null;
                cardImage.color = Color.white;
            }

            if (nameText != null)
            {
                nameText.text = context.DisplayName ?? string.Empty;
            }

            if (statsText != null)
            {
                statsText.text = context.IsRevealed
                    ? "HP " + context.CurrentHp + " / " + context.MaxHp + "\n공격 " + context.AttackPower
                    : string.Empty;
            }

            if (typeText != null)
            {
                typeText.text = context.IsRevealed ? context.TypeLabel ?? string.Empty : string.Empty;
            }

            if (contextText != null)
            {
                contextText.text = context.ContextLines ?? string.Empty;
            }
        }
    }
}
