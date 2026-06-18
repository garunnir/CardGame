using UnityEngine;

namespace CardGame.CardBattle.Cards
{
    /// <summary>카드 비주얼 폴백 — CardDataAsset 일러스트 미지정 시 사용.</summary>
    public static class CardVisualDefaults
    {
        private const string IllustrationResourcePath = "CardBattle/Art/CardIllustration_Default";

        private static Sprite illustrationPlaceholder;

        public static Sprite IllustrationPlaceholder
        {
            get
            {
                if (illustrationPlaceholder == null)
                {
                    illustrationPlaceholder = Resources.Load<Sprite>(IllustrationResourcePath);
                }

                return illustrationPlaceholder;
            }
        }

        public static Sprite ResolveIllustration(CardDataAsset data)
        {
            if (data == null)
            {
                return null;
            }

            return data.illustration != null ? data.illustration : IllustrationPlaceholder;
        }
    }
}
