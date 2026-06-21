using UnityEngine;

namespace CardGame.CardBattle.Cards
{
    /// <summary>카드 앞/뒤 면 — Quad Mesh + CardFaceUnlit. 일러스트는 높이 기준 fit(가로 크롭).</summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class CardFaceView : MonoBehaviour
    {
        public const float DefaultWidth = 1.6f;
        public const float DefaultHeight = 2.2f;
        public const float DefaultQuadAspect = DefaultWidth / DefaultHeight;
        public const float FrontFaceLocalZ = 0.012f;
        public const float BackFaceLocalZ = -0.012f;
        public const float LabelLocalZ = 0.02f;
        public const int NameLabelSortingOrder = 2;
        public const int HpLabelSortingOrder = 3;
        public const int FloatingTextSortingOrder = 8;
        public const int BattleVfxSortingOrder = 12;

        /// <summary>명중 VFX 스폰 — 카드 로컬 Y+ (중심 위쪽).</summary>
        public static readonly Vector3 BattleVfxOffsetLocal = new Vector3(0f, 0f, -2f);

        public const float BattleVfxFaceForwardOffset = 0.025f;

        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int MainTexStId = Shader.PropertyToID("_MainTex_ST");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int SpriteAspectId = Shader.PropertyToID("_SpriteAspect");
        private static readonly int QuadAspectId = Shader.PropertyToID("_QuadAspect");
        private static readonly int SpriteFitId = Shader.PropertyToID("_SpriteFit");

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
                propertyBlock.SetFloat(SpriteAspectId, rect.width / rect.height);
                propertyBlock.SetFloat(QuadAspectId, DefaultQuadAspect);
                propertyBlock.SetFloat(SpriteFitId, 1f);
            }

            renderer.SetPropertyBlock(propertyBlock);
        }
    }
}
