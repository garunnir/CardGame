using UnityEngine;

namespace CardGame.CardBattle.Cards
{
    /// <summary>카드 앞/뒤 면 — Quad Mesh + CardFaceUnlit, Sprite UV/틴트.</summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class CardFaceView : MonoBehaviour
    {
        public const float DefaultWidth = 1.6f;
        public const float DefaultHeight = 2.2f;

        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int MainTexStId = Shader.PropertyToID("_MainTex_ST");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private MeshRenderer meshRenderer;
        private MaterialPropertyBlock propertyBlock;
        private Sprite currentSprite;
        private Color baseColor = Color.white;

        public Color BaseColor => baseColor;

        private MeshRenderer Renderer
        {
            get
            {
                if (meshRenderer == null)
                {
                    meshRenderer = GetComponent<MeshRenderer>();
                }

                return meshRenderer;
            }
        }

        private void Awake()
        {
            _ = Renderer;
            propertyBlock ??= new MaterialPropertyBlock();
        }

        public void ApplySprite(Sprite sprite)
        {
            var renderer = Renderer;
            if (renderer == null)
            {
                return;
            }

            currentSprite = sprite;
            renderer.enabled = sprite != null;
            if (sprite != null)
            {
                RefreshPropertyBlock();
            }
        }

        public void SetColor(Color color)
        {
            baseColor = color;
            var renderer = Renderer;
            if (renderer != null && renderer.enabled)
            {
                RefreshPropertyBlock();
            }
        }

        private void RefreshPropertyBlock()
        {
            var renderer = Renderer;
            if (renderer == null)
            {
                return;
            }

            propertyBlock ??= new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(ColorId, baseColor);

            if (currentSprite != null)
            {
                var texture = currentSprite.texture;
                var rect = currentSprite.textureRect;
                propertyBlock.SetTexture(MainTexId, texture);
                propertyBlock.SetVector(
                    MainTexStId,
                    new Vector4(
                        rect.width / texture.width,
                        rect.height / texture.height,
                        rect.x / texture.width,
                        rect.y / texture.height));
            }

            renderer.SetPropertyBlock(propertyBlock);
        }
    }
}
