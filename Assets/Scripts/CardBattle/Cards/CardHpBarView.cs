using UnityEngine;

namespace CardGame.CardBattle.Cards
{
    /// <summary>카드 HP 막대 — 배경/채움 쿼드. 위치·크기는 프리팹 SSOT.</summary>
    public sealed class CardHpBarView : MonoBehaviour
    {
        public const float DefaultWidth = 1.2f;
        public const float DefaultHeight = 0.12f;
        public const float DefaultLocalY = -0.55f;
        public const float DefaultLocalZ = 0.015f;
        public const int BackgroundSortingOrder = 2;
        public const int FillSortingOrder = 3;

        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int SpriteFitId = Shader.PropertyToID("_SpriteFit");

        [SerializeField] private Transform fillRoot;
        [SerializeField] private MeshRenderer backgroundRenderer;
        [SerializeField] private MeshRenderer fillRenderer;
        [SerializeField] private Color backgroundColor = new Color(0.12f, 0.12f, 0.14f, 1f);
        [SerializeField] private Color fillColor = new Color(0.35f, 0.88f, 0.42f, 1f);
        [SerializeField] private float barWidth = DefaultWidth;

        private MaterialPropertyBlock backgroundBlock;
        private MaterialPropertyBlock fillBlock;
        private float fillRatio;

        public void ApplyColors(Color background, Color fill)
        {
            backgroundColor = background;
            fillColor = fill;
            RefreshColors();
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void SetFill(int currentHp, int maxHp)
        {
            fillRatio = maxHp > 0 ? Mathf.Clamp01((float)currentHp / maxHp) : 0f;
            ApplyFillScale();
        }

        private void Awake()
        {
            RefreshColors();
            ApplyFillScale();
        }

        private void RefreshColors()
        {
            ApplySolidColor(backgroundRenderer, backgroundColor, ref backgroundBlock);
            ApplySolidColor(fillRenderer, fillColor, ref fillBlock);
        }

        private void ApplyFillScale()
        {
            if (fillRoot == null)
            {
                return;
            }

            fillRoot.localScale = new Vector3(barWidth * fillRatio, DefaultHeight, 1f);
            fillRoot.localPosition = new Vector3(-barWidth * 0.5f * (1f - fillRatio), 0f, 0f);
        }

        private static void ApplySolidColor(
            MeshRenderer renderer,
            Color color,
            ref MaterialPropertyBlock propertyBlock)
        {
            if (renderer == null)
            {
                return;
            }

            propertyBlock ??= new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetTexture(MainTexId, Texture2D.whiteTexture);
            propertyBlock.SetColor(ColorId, color);
            propertyBlock.SetFloat(SpriteFitId, 0f);
            renderer.SetPropertyBlock(propertyBlock);
        }
    }
}
