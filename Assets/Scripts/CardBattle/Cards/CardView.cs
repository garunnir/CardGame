using System;
using System.Collections;
using System.Collections.Generic;
using CardGame.CardBattle.Input;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CardGame.CardBattle.Cards
{
    /// <summary>카드 UI 바인딩 및 연출 스텁. DOTween + 코루틴 이중 폴백.</summary>
    public class CardView : MonoBehaviour,
        IPointerClickHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler,
        IDragSource,
        IDropTarget,
        IDragHoverVisual
    {
        private static readonly List<RaycastResult> PointerRaycastResults = new List<RaycastResult>(16);

        [SerializeField] private Image illustrationImage;
        [SerializeField] private Slider hpSlider;
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Image dragHighlightImage;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform shakeRoot;
        [SerializeField] private float hpTweenDuration = 0.35f;
        [SerializeField] private float attackDashDistance = 40f;
        [SerializeField] private float attackDashDuration = 0.2f;
        [SerializeField] private Color dragValidColor = new Color(0.3f, 1f, 0.35f, 0.85f);
        [SerializeField] private Color dragInvalidColor = new Color(1f, 0.35f, 0.35f, 0.85f);

        private CardModel boundModel;
        private Vector3 homeLocalPosition;
        private Coroutine hpCoroutine;
        private bool useDotween = true;
        private bool suppressNextClick;
        private bool dragStarted;
        private Sprite fallbackIllustrationSprite;

        public CardModel BoundModel => boundModel;
        public object DragPayload => boundModel;
        public Transform DragTransform => transform;
        public bool CanBeginDrag => boundModel != null && boundModel.IsAlive;
        public object DropPayload => boundModel;
        public Transform DropTransform => transform;

        public event Action<CardView> Clicked;
        public event Action<CardView, Vector2> DragStarted;
        public event Action<CardView, CardView, Vector2> DragMoved;
        public event Action<CardView, CardView, Vector2> DragEnded;

        private void Awake()
        {
            if (shakeRoot == null)
            {
                shakeRoot = transform as RectTransform;
            }

            homeLocalPosition = shakeRoot.localPosition;

            try
            {
                DOTween.Init(false, true, LogBehaviour.ErrorsOnly);
            }
            catch (Exception)
            {
                useDotween = false;
            }

            if (dragHighlightImage != null)
            {
                dragHighlightImage.enabled = false;
            }

            if (illustrationImage != null)
            {
                fallbackIllustrationSprite = illustrationImage.sprite;
            }
        }

        public void Bind(CardModel model)
        {
            boundModel = model;
            if (nameText != null)
            {
                nameText.text = model.DisplayName;
            }

            if (illustrationImage != null)
            {
                var sprite = model.Data != null && model.Data.illustration != null
                    ? model.Data.illustration
                    : fallbackIllustrationSprite;
                illustrationImage.sprite = sprite;
                illustrationImage.enabled = true;
            }

            RefreshHpInstant();
        }

        public void RefreshHpInstant()
        {
            if (boundModel == null || hpSlider == null)
            {
                return;
            }

            hpSlider.maxValue = boundModel.MaxHp;
            hpSlider.value = boundModel.CurrentHp;
            UpdateHpLabel(boundModel.CurrentHp);
        }

        public void PlayHpChange(int fromHp, int toHp, Action onComplete = null)
        {
            if (!CanRunCoroutines())
            {
                ApplyHpInstant(toHp);
                onComplete?.Invoke();
                return;
            }

            if (hpCoroutine != null)
            {
                StopCoroutine(hpCoroutine);
            }

            hpCoroutine = StartCoroutine(AnimateHpRoutine(fromHp, toHp, onComplete));
        }

        private IEnumerator AnimateHpRoutine(int fromHp, int toHp, Action onComplete)
        {
            if (useDotween && hpSlider != null)
            {
                var tween = DOTween.To(
                    () => hpSlider.value,
                    v =>
                    {
                        hpSlider.value = v;
                        UpdateHpLabel(Mathf.RoundToInt(v));
                    },
                    toHp,
                    hpTweenDuration).SetEase(Ease.OutQuad);

                yield return tween.WaitForCompletion();
            }
            else
            {
                var elapsed = 0f;
                while (elapsed < hpTweenDuration)
                {
                    elapsed += Time.deltaTime;
                    var t = Mathf.Clamp01(elapsed / hpTweenDuration);
                    var value = Mathf.Lerp(fromHp, toHp, t);
                    if (hpSlider != null)
                    {
                        hpSlider.value = value;
                    }

                    UpdateHpLabel(Mathf.RoundToInt(value));
                    yield return null;
                }
            }

            UpdateHpLabel(toHp);
            onComplete?.Invoke();
        }

        public void PlayAttackDash(Vector3 worldTarget, Action onComplete = null)
        {
            PlayAttackDash(worldTarget, 0f, null, onComplete);
        }

        public void PlayAttackDash(
            Vector3 worldTarget,
            float dashDuration,
            Action onImpact,
            Action onComplete = null)
        {
            if (shakeRoot == null)
            {
                onImpact?.Invoke();
                onComplete?.Invoke();
                return;
            }

            if (!CanRunCoroutines())
            {
                onImpact?.Invoke();
                onComplete?.Invoke();
                return;
            }

            var duration = dashDuration > 0f ? dashDuration : attackDashDuration;
            var half = duration * 0.5f;
            var direction = (worldTarget - shakeRoot.position).normalized;
            var targetLocal = homeLocalPosition + (Vector3)(direction * attackDashDistance);

            if (useDotween)
            {
                var seq = DOTween.Sequence();
                seq.Append(shakeRoot.DOLocalMove(targetLocal, half).SetEase(Ease.OutQuad));
                seq.AppendCallback(() => onImpact?.Invoke());
                seq.Append(shakeRoot.DOLocalMove(homeLocalPosition, half).SetEase(Ease.InQuad));
                seq.OnComplete(() => onComplete?.Invoke());
            }
            else
            {
                StartCoroutine(AttackDashCoroutine(targetLocal, half, onImpact, onComplete));
            }
        }

        private IEnumerator AttackDashCoroutine(
            Vector3 targetLocal,
            float halfDuration,
            Action onImpact,
            Action onComplete)
        {
            yield return MoveLocalRoutine(shakeRoot.localPosition, targetLocal, halfDuration);
            onImpact?.Invoke();
            yield return MoveLocalRoutine(targetLocal, homeLocalPosition, halfDuration);
            onComplete?.Invoke();
        }

        public void PlayHitShake(Action onComplete = null)
        {
            PlayHitShake(0f, onComplete);
        }

        public void PlayHitShake(float strength, Action onComplete = null)
        {
            var shakeStrength = strength > 0f
                ? new Vector3(strength, strength * 0.66f, 0f)
                : new Vector3(12f, 8f, 0f);

            if (!CanRunCoroutines())
            {
                if (shakeRoot != null)
                {
                    shakeRoot.localPosition = homeLocalPosition;
                }

                onComplete?.Invoke();
                return;
            }

            if (useDotween && shakeRoot != null)
            {
                shakeRoot.DOShakePosition(0.25f, shakeStrength, 20, 90f, false, true)
                    .OnComplete(() =>
                    {
                        shakeRoot.localPosition = homeLocalPosition;
                        onComplete?.Invoke();
                    });
            }
            else
            {
                StartCoroutine(ShakeCoroutine(onComplete));
            }
        }

        private IEnumerator ShakeCoroutine(Action onComplete)
        {
            const int shakes = 6;
            for (var i = 0; i < shakes; i++)
            {
                shakeRoot.localPosition = homeLocalPosition + new Vector3(
                    UnityEngine.Random.Range(-8f, 8f),
                    UnityEngine.Random.Range(-6f, 6f),
                    0f);
                yield return new WaitForSeconds(0.03f);
            }

            shakeRoot.localPosition = homeLocalPosition;
            onComplete?.Invoke();
        }

        public void PlayDrawIn(Vector3 fromWorld, Action onComplete = null)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            transform.position = fromWorld;

            if (!CanRunCoroutines())
            {
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                }

                onComplete?.Invoke();
                return;
            }

            if (useDotween)
            {
                var seq = DOTween.Sequence();
                if (canvasGroup != null)
                {
                    seq.Join(DOTween.To(
                        () => canvasGroup.alpha,
                        x => canvasGroup.alpha = x,
                        1f,
                        0.35f));
                }

                if (shakeRoot != null)
                {
                    seq.Join(shakeRoot.DOLocalMove(homeLocalPosition, 0.45f).SetEase(Ease.OutCubic));
                }
                seq.OnComplete(() => onComplete?.Invoke());
            }
            else
            {
                StartCoroutine(DrawInCoroutine(onComplete));
            }
        }

        private IEnumerator DrawInCoroutine(Action onComplete)
        {
            var start = transform.position;
            var end = shakeRoot != null ? shakeRoot.position : transform.position;
            var elapsed = 0f;
            const float duration = 0.45f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                transform.position = Vector3.Lerp(start, end, t);
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = t;
                }

                yield return null;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            onComplete?.Invoke();
        }

        /// <summary>URP Volume(Bloom 등) 연동용 훅.</summary>
        public virtual void TriggerUrppostFx(string fxId, float intensity)
        {
            // BattleBridge / UIManager에서 VolumeProfile 제어
        }

        public void OnClick()
        {
            Clicked?.Invoke(this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (suppressNextClick)
            {
                suppressNextClick = false;
                return;
            }

            OnClick();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !CanBeginDrag)
            {
                dragStarted = false;
                return;
            }

            dragStarted = true;
            DragStarted?.Invoke(this, eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragStarted)
            {
                return;
            }

            DragMoved?.Invoke(this, FindHoveredCardView(eventData), eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!dragStarted)
            {
                return;
            }

            dragStarted = false;
            suppressNextClick = true;
            DragEnded?.Invoke(this, FindHoveredCardView(eventData), eventData.position);
        }

        public void SetHoverState(bool isActive, bool isValid)
        {
            if (dragHighlightImage == null)
            {
                return;
            }

            dragHighlightImage.enabled = isActive;
            if (isActive)
            {
                dragHighlightImage.color = isValid ? dragValidColor : dragInvalidColor;
            }
        }

        private IEnumerator MoveLocalRoutine(Vector3 from, Vector3 to, float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                shakeRoot.localPosition = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            shakeRoot.localPosition = to;
        }

        private void UpdateHpLabel(int hp)
        {
            if (hpText != null)
            {
                hpText.text = hp.ToString();
            }
        }

        private bool CanRunCoroutines()
        {
            return isActiveAndEnabled && gameObject.activeInHierarchy;
        }

        private void ApplyHpInstant(int hp)
        {
            if (hpSlider != null)
            {
                hpSlider.value = hp;
            }

            UpdateHpLabel(hp);
        }

        public void SetHomePosition(Vector3 localPos)
        {
            homeLocalPosition = localPos;
            if (shakeRoot != null)
            {
                shakeRoot.localPosition = localPos;
            }
        }

        private CardView FindHoveredCardView(PointerEventData eventData)
        {
            if (EventSystem.current == null || eventData == null)
            {
                return null;
            }

            PointerRaycastResults.Clear();
            EventSystem.current.RaycastAll(eventData, PointerRaycastResults);
            for (var i = 0; i < PointerRaycastResults.Count; i++)
            {
                var hitObject = PointerRaycastResults[i].gameObject;
                if (hitObject == null)
                {
                    continue;
                }

                var candidate = hitObject.GetComponentInParent<CardView>();
                if (candidate != null && candidate != this)
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
