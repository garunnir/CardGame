using System;
using System.Collections;
using System.Collections.Generic;
using CardGame.CardBattle.Cards;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CardGame.CardBattle.Cards
{
    /// <summary>카드 UI 바인딩 및 연출 스텁. DOTween + 코루틴 이중 폴백.</summary>
    public class CardView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image illustrationImage;
        [SerializeField] private Slider hpSlider;
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform shakeRoot;
        [SerializeField] private float hpTweenDuration = 0.35f;
        [SerializeField] private float attackDashDistance = 40f;
        [SerializeField] private float attackDashDuration = 0.2f;

        private CardModel boundModel;
        private Vector3 homeLocalPosition;
        private Coroutine hpCoroutine;
        private bool useDotween = true;

        public CardModel BoundModel => boundModel;
        public event Action<CardView> Clicked;

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
        }

        public void Bind(CardModel model)
        {
            boundModel = model;
            if (nameText != null)
            {
                nameText.text = model.DisplayName;
            }

            if (illustrationImage != null && model.Data.illustration != null)
            {
                illustrationImage.sprite = model.Data.illustration;
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
            if (shakeRoot == null)
            {
                onComplete?.Invoke();
                return;
            }

            var direction = (worldTarget - shakeRoot.position).normalized;
            var targetLocal = homeLocalPosition + (Vector3)(direction * attackDashDistance);

            if (useDotween)
            {
                var seq = DOTween.Sequence();
                seq.Append(shakeRoot.DOLocalMove(targetLocal, attackDashDuration).SetEase(Ease.OutQuad));
                seq.Append(shakeRoot.DOLocalMove(homeLocalPosition, attackDashDuration).SetEase(Ease.InQuad));
                seq.OnComplete(() => onComplete?.Invoke());
            }
            else
            {
                StartCoroutine(AttackDashCoroutine(targetLocal, onComplete));
            }
        }

        private IEnumerator AttackDashCoroutine(Vector3 targetLocal, Action onComplete)
        {
            yield return MoveLocalRoutine(shakeRoot.localPosition, targetLocal, attackDashDuration);
            yield return MoveLocalRoutine(targetLocal, homeLocalPosition, attackDashDuration);
            onComplete?.Invoke();
        }

        public void PlayHitShake(Action onComplete = null)
        {
            if (useDotween && shakeRoot != null)
            {
                shakeRoot.DOShakePosition(0.25f, new Vector3(12f, 8f, 0f), 20, 90f, false, true)
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
            OnClick();
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

        public void SetHomePosition(Vector3 localPos)
        {
            homeLocalPosition = localPos;
            if (shakeRoot != null)
            {
                shakeRoot.localPosition = localPos;
            }
        }
    }
}
