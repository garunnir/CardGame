using UnityEngine;

namespace CardGame.CardBattle.Presentation
{
    /// <summary>UGUI Image 폴백 스프라이트 — 내장 UISprite 미포함 빌드 대응.</summary>
    public static class UiImageDefaults
    {
        private static Sprite whiteSprite;

        public static Sprite WhiteSprite
        {
            get
            {
                if (whiteSprite != null)
                {
                    return whiteSprite;
                }

                var texture = Texture2D.whiteTexture;
                whiteSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
                return whiteSprite;
            }
        }
    }
}
