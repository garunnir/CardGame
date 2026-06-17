using UnityEngine;
using UnityEngine.UI;

namespace CardGame.CardBattle.Input
{
    /// <summary>도메인 타입과 분리된 드래그 프리뷰 렌더러.</summary>
    public sealed class DragTargetingPresenter : MonoBehaviour
    {
        [SerializeField] private RectTransform lineRect;
        [SerializeField] private Image lineImage;
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private Camera uiCamera;
        [SerializeField] private float lineThickness = 6f;
        [SerializeField] private Color idleColor = new Color(1f, 1f, 1f, 0.85f);
        [SerializeField] private Color validColor = new Color(0.3f, 1f, 0.35f, 0.9f);
        [SerializeField] private Color invalidColor = new Color(1f, 0.35f, 0.35f, 0.9f);

        private Transform activeSource;
        private IDragHoverVisual activeHoverVisual;

        private void Awake()
        {
            HideLine();
        }

        public void BeginDrag(Transform source)
        {
            activeSource = source;
            SetHoverVisual(null, false);
            ShowLine(idleColor);
        }

        public void UpdateDrag(Vector2 pointerScreenPosition, Transform hoverTarget, bool isValidHover)
        {
            if (activeSource == null)
            {
                return;
            }

            var sourceScreen = RectTransformUtility.WorldToScreenPoint(uiCamera, activeSource.position);
            var lineColor = idleColor;
            if (hoverTarget != null)
            {
                lineColor = isValidHover ? validColor : invalidColor;
            }

            DrawLine(sourceScreen, pointerScreenPosition, lineColor);
            SetHoverVisual(ResolveHoverVisual(hoverTarget), isValidHover);
        }

        public void EndDrag()
        {
            activeSource = null;
            SetHoverVisual(null, false);
            HideLine();
        }

        private void DrawLine(Vector2 fromScreen, Vector2 toScreen, Color color)
        {
            if (lineRect == null)
            {
                return;
            }

            var canvasRect = rootCanvas != null
                ? rootCanvas.transform as RectTransform
                : lineRect.parent as RectTransform;
            if (canvasRect == null)
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    fromScreen,
                    uiCamera,
                    out var fromLocal))
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    toScreen,
                    uiCamera,
                    out var toLocal))
            {
                return;
            }

            var delta = toLocal - fromLocal;
            var length = Mathf.Max(1f, delta.magnitude);
            var angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

            lineRect.gameObject.SetActive(true);
            lineRect.anchoredPosition = fromLocal + (delta * 0.5f);
            lineRect.sizeDelta = new Vector2(length, lineThickness);
            lineRect.localRotation = Quaternion.Euler(0f, 0f, angle);

            if (lineImage != null)
            {
                lineImage.color = color;
            }
        }

        private void ShowLine(Color color)
        {
            if (lineRect == null)
            {
                return;
            }

            lineRect.gameObject.SetActive(true);
            if (lineImage != null)
            {
                lineImage.color = color;
            }
        }

        private void HideLine()
        {
            if (lineRect != null)
            {
                lineRect.gameObject.SetActive(false);
            }
        }

        private void SetHoverVisual(IDragHoverVisual nextHover, bool isValid)
        {
            if (activeHoverVisual != null && activeHoverVisual != nextHover)
            {
                activeHoverVisual.SetHoverState(false, false);
            }

            activeHoverVisual = nextHover;
            if (activeHoverVisual != null)
            {
                activeHoverVisual.SetHoverState(true, isValid);
            }
        }

        private static IDragHoverVisual ResolveHoverVisual(Transform hoverTarget)
        {
            if (hoverTarget == null)
            {
                return null;
            }

            return hoverTarget.GetComponentInParent<IDragHoverVisual>();
        }
    }
}
