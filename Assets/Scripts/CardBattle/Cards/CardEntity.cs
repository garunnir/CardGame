using System;
using System.Collections;
using System.Collections.Generic;
using CardGame.CardBattle.Input;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CardGame.CardBattle.Cards
{
    public enum CardBoardPhase
    {
        Hidden,
        ReserveFaceDown,
        BattlefieldFaceUp
    }

    /// <summary>3D 카드 엔티티 — 앞/뒤 쿼드, 배치 플립, 드래그 타겟팅. 히트는 ShakeRoot 콜라이더.</summary>
    public sealed class CardEntity : MonoBehaviour,
        ICardBattleView,
        ICardInputHost,
        IDragSource,
        IDropTarget,
        IDragHoverVisual,
        IPointerClickHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        private static readonly List<RaycastResult> PointerRaycastResults = new List<RaycastResult>(16);

        [SerializeField] private Transform shakeRoot;
        [SerializeField] private CardFaceView frontFace;
        [SerializeField] private CardFaceView backFace;
        [SerializeField] private TextMeshPro nameLabel;
        [SerializeField] private TextMeshPro hpLabel;
        [SerializeField] private float attackDashDistance = 0.55f;
        [SerializeField] private float attackDashDuration = 0.2f;
        [SerializeField] private float deathVisualDuration = 0.35f;
        [SerializeField] private Color hoverValidColor = new Color(0.55f, 1f, 0.6f, 1f);
        [SerializeField] private Color hoverInvalidColor = new Color(1f, 0.45f, 0.45f, 1f);

        private CardModel boundModel;
        private Vector3 homeLocalPosition;
        private Quaternion homeLocalRotation;
        private Color frontBaseColor = Color.white;
        private Color backBaseColor = Color.white;
        private bool useDotween = true;
        private bool suppressNextClick;
        private bool dragStarted;
        private CardBoardPhase phase = CardBoardPhase.Hidden;
        private Coroutine hpCoroutine;

        private bool IsMotionValid => this != null && shakeRoot != null;

        public CardModel BoundModel => boundModel;
        public CardBoardPhase Phase => phase;
        public Transform ViewTransform => transform;
        public Transform InputTransform => transform;
        public object DragPayload => boundModel;
        public Transform DragTransform => transform;
        public bool CanBeginDrag => CanAcceptTarget && boundModel != null && boundModel.IsPlayerTeam;
        public bool CanAcceptTarget => boundModel != null && boundModel.IsAlive && phase == CardBoardPhase.BattlefieldFaceUp;
        public object DropPayload => boundModel;
        public Transform DropTransform => transform;

        public event Action<ICardInputHost> Clicked;
        public event Action<ICardInputHost, Vector2> DragStarted;
        public event Action<ICardInputHost, ICardInputHost, Vector2> DragMoved;
        public event Action<ICardInputHost, ICardInputHost, Vector2> DragEnded;

        private void Awake()
        {
            if (shakeRoot == null)
            {
                shakeRoot = transform;
            }

            EnsureHitColliderOnShakeRoot();

            homeLocalPosition = transform.localPosition;
            homeLocalRotation = transform.localRotation;

            if (frontFace != null)
            {
                frontBaseColor = frontFace.BaseColor;
            }

            if (backFace != null)
            {
                backBaseColor = backFace.BaseColor;
            }

            try
            {
                DOTween.Init(false, true, LogBehaviour.ErrorsOnly);
            }
            catch (Exception)
            {
                useDotween = false;
            }
        }

        private void OnDestroy()
        {
            transform.DOKill();
            if (shakeRoot != null && shakeRoot != transform)
            {
                shakeRoot.DOKill();
            }
        }

        public void CancelMotion()
        {
            transform.DOKill();
            if (shakeRoot != null && shakeRoot != transform)
            {
                shakeRoot.DOKill();
                shakeRoot.localPosition = Vector3.zero;
            }

            transform.localRotation = homeLocalRotation;
        }

        public void ApplyBackSprite(Sprite backSprite)
        {
            if (backFace != null && backSprite != null)
            {
                backFace.ApplySprite(backSprite);
            }
        }

        public void Bind(CardModel model)
        {
            boundModel = model;
            if (nameLabel != null)
            {
                nameLabel.text = model != null ? model.DisplayName : string.Empty;
            }

            if (frontFace != null)
            {
                frontFace.ApplySprite(CardVisualDefaults.ResolveIllustration(model?.Data));
            }

            RefreshHpInstant();
        }

        private void EnsureHitColliderOnShakeRoot()
        {
            var legacyRootCollider = GetComponent<BoxCollider>();
            if (legacyRootCollider != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(legacyRootCollider);
                }
                else
                {
                    DestroyImmediate(legacyRootCollider);
                }
            }

            var hit = shakeRoot.GetComponent<BoxCollider>();
            if (hit == null)
            {
                hit = shakeRoot.gameObject.AddComponent<BoxCollider>();
            }

            hit.center = Vector3.zero;
            hit.size = new Vector3(CardFaceView.DefaultWidth, CardFaceView.DefaultHeight, 0.08f);
        }

        public void RefreshHpInstant()
        {
            if (boundModel == null || hpLabel == null)
            {
                return;
            }

            hpLabel.text = boundModel.CurrentHp.ToString();
        }

        public void SetPhase(CardBoardPhase nextPhase)
        {
            phase = nextPhase;
            switch (phase)
            {
                case CardBoardPhase.Hidden:
                    gameObject.SetActive(false);
                    break;
                case CardBoardPhase.ReserveFaceDown:
                    gameObject.SetActive(true);
                    SetFaceDownInstant();
                    SetFrontLabelsVisible(false);
                    break;
                case CardBoardPhase.BattlefieldFaceUp:
                    gameObject.SetActive(true);
                    SetFaceUpInstant();
                    SetFrontLabelsVisible(true);
                    break;
            }
        }

        public void SnapToLocalPosition(Vector3 localPosition)
        {
            SnapToLocalPose(localPosition, homeLocalRotation);
        }

        public void SnapToAnchorPose(Vector3 localPosition)
        {
            SnapToLocalPose(localPosition, Quaternion.identity);
        }

        public async UniTask PlayDeployOnAnchorAsync(
            Vector3 targetLocalPosition,
            float moveDuration,
            float flipDuration)
        {
            await PlayDeployAsync(targetLocalPosition, Quaternion.identity, moveDuration, flipDuration);
        }

        public UniTask SnapReserveOnAnchorAsync(Vector3 localPosition)
        {
            return SnapReserveAsync(localPosition, Quaternion.identity);
        }

        public void SnapToLocalPose(Vector3 localPosition, Quaternion localRotation)
        {
            CancelMotion();
            homeLocalPosition = localPosition;
            homeLocalRotation = localRotation;
            transform.localPosition = localPosition;
            transform.localRotation = localRotation;
            ResetVisualOffset();
        }

        public async UniTask PlayDeployAsync(
            Vector3 targetLocalPosition,
            Quaternion targetLocalRotation,
            float moveDuration,
            float flipDuration)
        {
            if (!IsMotionValid)
            {
                return;
            }

            var token = this.GetCancellationTokenOnDestroy();
            gameObject.SetActive(true);
            ResetVisualScale();
            transform.localRotation = targetLocalRotation;
            SetFaceDownInstant();
            SetFrontLabelsVisible(false);

            var moveTask = AnimateLocalMoveAsync(transform.localPosition, targetLocalPosition, moveDuration, token);
            var flipTask = AnimateFlipFaceUpAsync(flipDuration, token);
            await UniTask.WhenAll(moveTask, flipTask);

            if (!IsMotionValid || token.IsCancellationRequested)
            {
                return;
            }

            homeLocalPosition = targetLocalPosition;
            homeLocalRotation = targetLocalRotation;
            transform.localPosition = targetLocalPosition;
            transform.localRotation = targetLocalRotation;
            ResetVisualOffset();
            phase = CardBoardPhase.BattlefieldFaceUp;
            SetFrontLabelsVisible(true);
            RefreshHpInstant();
        }

        public UniTask SnapReserveAsync(Vector3 localPosition, Quaternion localRotation)
        {
            if (!IsMotionValid)
            {
                return UniTask.CompletedTask;
            }

            CancelMotion();
            homeLocalPosition = localPosition;
            homeLocalRotation = localRotation;
            transform.localPosition = localPosition;
            transform.localRotation = localRotation;
            ResetVisualOffset();
            ResetVisualScale();
            phase = CardBoardPhase.ReserveFaceDown;
            SetFaceDownInstant();
            SetFrontLabelsVisible(false);
            gameObject.SetActive(true);
            return UniTask.CompletedTask;
        }

        public void PlayHpChange(int fromHp, int toHp, Action onComplete = null)
        {
            if (!isActiveAndEnabled || hpLabel == null)
            {
                RefreshHpInstant();
                onComplete?.Invoke();
                return;
            }

            if (hpCoroutine != null)
            {
                StopCoroutine(hpCoroutine);
            }

            hpCoroutine = StartCoroutine(AnimateHpRoutine(fromHp, toHp, onComplete));
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

            var duration = dashDuration > 0f ? dashDuration : attackDashDuration;
            var half = duration * 0.5f;
            var direction = (worldTarget - transform.position).normalized;
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = transform.forward;
            }

            var dashOffset = transform.InverseTransformDirection(direction) * attackDashDistance;

            if (useDotween)
            {
                var seq = DOTween.Sequence();
                seq.Append(shakeRoot.DOLocalMove(dashOffset, half).SetEase(Ease.OutQuad));
                seq.AppendCallback(() => onImpact?.Invoke());
                seq.Append(shakeRoot.DOLocalMove(Vector3.zero, half).SetEase(Ease.InQuad));
                seq.OnComplete(() => onComplete?.Invoke());
            }
            else
            {
                StartCoroutine(AttackDashCoroutine(dashOffset, half, onImpact, onComplete));
            }
        }

        public void PlayHitShake(float strength, Action onComplete = null)
        {
            var shakeStrength = strength > 0f
                ? new Vector3(strength * 0.05f, strength * 0.035f, 0f)
                : new Vector3(0.08f, 0.05f, 0f);

            if (shakeRoot == null)
            {
                onComplete?.Invoke();
                return;
            }

            if (useDotween)
            {
                shakeRoot.DOShakePosition(0.25f, shakeStrength, 20, 90f, false, true)
                    .OnComplete(() =>
                    {
                        ResetVisualOffset();
                        onComplete?.Invoke();
                    });
            }
            else
            {
                StartCoroutine(ShakeCoroutine(onComplete));
            }
        }

        public void PlayDeathVisual(Action onComplete = null)
        {
            if (!isActiveAndEnabled)
            {
                onComplete?.Invoke();
                return;
            }

            CancelMotion();
            SetFrontLabelsVisible(false);

            if (shakeRoot == null)
            {
                gameObject.SetActive(false);
                onComplete?.Invoke();
                return;
            }

            if (useDotween)
            {
                shakeRoot.DOScale(Vector3.zero, deathVisualDuration)
                    .SetEase(Ease.InBack)
                    .OnComplete(() =>
                    {
                        ResetVisualScale();
                        gameObject.SetActive(false);
                        onComplete?.Invoke();
                    });
            }
            else
            {
                gameObject.SetActive(false);
                onComplete?.Invoke();
            }
        }

        public void SetHoverState(bool isActive, bool isValid)
        {
            if (isActive)
            {
                var tint = isValid ? hoverValidColor : hoverInvalidColor;
                if (frontFace != null)
                {
                    frontFace.SetColor(tint);
                }

                if (backFace != null)
                {
                    backFace.SetColor(tint);
                }

                return;
            }

            if (frontFace != null)
            {
                frontFace.SetColor(frontBaseColor);
            }

            if (backFace != null)
            {
                backFace.SetColor(backBaseColor);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (suppressNextClick)
            {
                suppressNextClick = false;
                return;
            }

            Clicked?.Invoke(this);
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

            DragMoved?.Invoke(this, FindHoveredHost(eventData), eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!dragStarted)
            {
                return;
            }

            dragStarted = false;
            suppressNextClick = true;
            DragEnded?.Invoke(this, FindHoveredHost(eventData), eventData.position);
        }

        private void SetFaceDownInstant()
        {
            if (!IsMotionValid)
            {
                return;
            }

            shakeRoot.localRotation = Quaternion.Euler(0f, 180f, 0f);
        }

        private void SetFaceUpInstant()
        {
            if (!IsMotionValid)
            {
                return;
            }

            shakeRoot.localRotation = Quaternion.identity;
        }

        private void SetFrontLabelsVisible(bool visible)
        {
            if (nameLabel != null)
            {
                nameLabel.gameObject.SetActive(visible);
            }

            if (hpLabel != null)
            {
                hpLabel.gameObject.SetActive(visible);
            }
        }

        private async UniTask AnimateLocalMoveAsync(
            Vector3 from,
            Vector3 to,
            float duration,
            System.Threading.CancellationToken token)
        {
            if (!IsMotionValid)
            {
                return;
            }

            if (duration <= 0f)
            {
                transform.localPosition = to;
                return;
            }

            if (useDotween)
            {
                var tween = transform.DOLocalMove(to, duration).SetEase(Ease.OutCubic);
                await UniTask.WaitUntil(() => !tween.IsActive(), cancellationToken: token);
                return;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                if (!IsMotionValid || token.IsCancellationRequested)
                {
                    return;
                }

                elapsed += Time.deltaTime;
                transform.localPosition = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                await UniTask.Yield(token);
            }

            if (IsMotionValid)
            {
                transform.localPosition = to;
            }
        }

        private async UniTask AnimateFlipFaceUpAsync(float duration, System.Threading.CancellationToken token)
        {
            if (!IsMotionValid)
            {
                return;
            }

            if (duration <= 0f)
            {
                SetFaceUpInstant();
                return;
            }

            const float startY = 180f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                if (!IsMotionValid || token.IsCancellationRequested)
                {
                    return;
                }

                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var y = Mathf.Lerp(startY, 0f, t);
                shakeRoot.localRotation = Quaternion.Euler(0f, y, 0f);
                await UniTask.Yield(token);
            }

            SetFaceUpInstant();
        }

        private IEnumerator AnimateHpRoutine(int fromHp, int toHp, Action onComplete)
        {
            const float duration = 0.35f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var value = Mathf.RoundToInt(Mathf.Lerp(fromHp, toHp, Mathf.Clamp01(elapsed / duration)));
                if (hpLabel != null)
                {
                    hpLabel.text = value.ToString();
                }

                yield return null;
            }

            if (hpLabel != null)
            {
                hpLabel.text = toHp.ToString();
            }

            onComplete?.Invoke();
        }

        private IEnumerator AttackDashCoroutine(
            Vector3 dashOffset,
            float halfDuration,
            Action onImpact,
            Action onComplete)
        {
            yield return MoveVisualOffsetRoutine(Vector3.zero, dashOffset, halfDuration);
            onImpact?.Invoke();
            yield return MoveVisualOffsetRoutine(dashOffset, Vector3.zero, halfDuration);
            onComplete?.Invoke();
        }

        private IEnumerator ShakeCoroutine(Action onComplete)
        {
            const int shakes = 6;
            for (var i = 0; i < shakes; i++)
            {
                shakeRoot.localPosition = new Vector3(
                    UnityEngine.Random.Range(-0.04f, 0.04f),
                    UnityEngine.Random.Range(-0.03f, 0.03f),
                    0f);
                yield return new WaitForSeconds(0.03f);
            }

            ResetVisualOffset();
            onComplete?.Invoke();
        }

        private IEnumerator MoveVisualOffsetRoutine(Vector3 from, Vector3 to, float duration)
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

        private void ResetVisualOffset()
        {
            if (shakeRoot != null && shakeRoot != transform)
            {
                shakeRoot.localPosition = Vector3.zero;
            }
        }

        private void ResetVisualScale()
        {
            if (shakeRoot != null)
            {
                shakeRoot.localScale = Vector3.one;
            }
        }

        private ICardInputHost FindHoveredHost(PointerEventData eventData)
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

                if (hitObject.transform.IsChildOf(transform))
                {
                    continue;
                }

                var candidate = hitObject.GetComponentInParent<CardEntity>();
                if (candidate != null && candidate != this && candidate.CanAcceptTarget)
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
