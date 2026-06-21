using UnityEngine;

namespace CardGame.CardBattle.Cards
{
    /// <summary>영웅 HP+보호막 합산 3D 바.</summary>
    public sealed class HeroHpShieldBarView : MonoBehaviour
    {
        public const float DefaultWidth = 1.2f;
        public const float DefaultHeight = 0.12f;
        public const float DefaultLocalY = -0.55f;
        public const float DefaultLocalZ = HeroVisualSorting.BarLocalZ;
        public const int BackgroundSortingOrder = HeroVisualSorting.BarBackground;
        public const int HpFillSortingOrder = HeroVisualSorting.BarFill;
        public const int ShieldFillSortingOrder = HeroVisualSorting.BarFill;

        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int SpriteFitId = Shader.PropertyToID("_SpriteFit");

        private static readonly Color HpFillColor = new Color(0.35f, 0.88f, 0.42f, 1f);
        private static readonly Color ShieldFillColor = new Color(0.35f, 0.75f, 0.95f, 1f);
        private static readonly Color TrackColor = new Color(0.12f, 0.12f, 0.14f, 1f);

        [SerializeField] private Transform hpFillRoot;
        [SerializeField] private Transform shieldFillRoot;
        [SerializeField] private MeshRenderer backgroundRenderer;
        [SerializeField] private MeshRenderer hpFillRenderer;
        [SerializeField] private MeshRenderer shieldFillRenderer;
        [SerializeField] private float barWidth = DefaultWidth;

        private MaterialPropertyBlock backgroundBlock;
        private MaterialPropertyBlock hpFillBlock;
        private MaterialPropertyBlock shieldFillBlock;

        private void Awake()
        {
            ApplySolidColor(backgroundRenderer, TrackColor, ref backgroundBlock);
            ApplySolidColor(hpFillRenderer, HpFillColor, ref hpFillBlock);
            ApplySolidColor(shieldFillRenderer, ShieldFillColor, ref shieldFillBlock);
            SetFill(0, 0, 1);
        }

        public void SetFill(int hp, int shield, int maxHp)
        {
            var safeMax = maxHp > 0 ? maxHp : 1;
            var hpRatio = Mathf.Clamp01((float)hp / safeMax);
            var shieldRatio = Mathf.Clamp01((float)shield / safeMax);
            var total = hpRatio + shieldRatio;
            if (total > 1f)
            {
                hpRatio /= total;
                shieldRatio /= total;
            }

            ApplySegment(hpFillRoot, 0f, hpRatio);
            ApplySegment(shieldFillRoot, hpRatio, hpRatio + shieldRatio);
        }

        public void ApplySortingOrders()
        {
            ApplyRendererOrder(backgroundRenderer, BackgroundSortingOrder);
            ApplyRendererOrder(hpFillRenderer, HpFillSortingOrder);
            ApplyRendererOrder(shieldFillRenderer, ShieldFillSortingOrder);
        }

        private static void ApplyRendererOrder(MeshRenderer renderer, int order)
        {
            if (renderer != null)
            {
                renderer.sortingOrder = order;
            }
        }

        private void ApplySegment(Transform fillRoot, float minX, float maxX)
        {
            if (fillRoot == null)
            {
                return;
            }

            var segmentWidth = (maxX - minX) * barWidth;
            var centerX = (-barWidth * 0.5f) + ((minX + maxX) * 0.5f * barWidth);
            fillRoot.localScale = new Vector3(Mathf.Max(segmentWidth, 0.0001f), DefaultHeight, 1f);
            fillRoot.localPosition = new Vector3(centerX, 0f, 0f);
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
