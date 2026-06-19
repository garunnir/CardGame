using System;
using System.Collections;
using CardGame.CardBattle.Core;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace CardGame.CardBattle.Cards
{
    public sealed partial class CardEntity
    {
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

        /// <summary>목표 앵커·phase를 적용. reparent·보간은 View가 현재 월드 pose 기준으로 처리.</summary>
        public async UniTask ApplyPlacement(
            Transform anchorParent,
            Vector3 targetLocalPosition,
            Quaternion targetLocalRotation,
            CardBoardPhase targetPhase,
            bool animate)
        {
            if (!IsMotionValid)
            {
                return;
            }

            CancelMotion();
            ReparentPreserveWorld(anchorParent);

            switch (targetPhase)
            {
                case CardBoardPhase.ReserveFaceDown:
                    await ApplyReservePlacementAsync(targetLocalPosition, targetLocalRotation, animate);
                    break;
                case CardBoardPhase.BattlefieldFaceUp:
                    await ApplyBattlefieldPlacementAsync(targetLocalPosition, targetLocalRotation, animate);
                    break;
            }
        }

        public async UniTask PlayRealignOnAnchorAsync(Vector3 targetLocalPosition, float moveDuration)
        {
            if (!IsMotionValid)
            {
                return;
            }

            if (IsAtHomeLocalPosition(targetLocalPosition))
            {
                return;
            }

            var token = this.GetCancellationTokenOnDestroy();
            await AnimateLocalMoveAsync(transform.localPosition, targetLocalPosition, moveDuration, token);

            if (!IsMotionValid || token.IsCancellationRequested)
            {
                return;
            }

            homeLocalPosition = targetLocalPosition;
            homeLocalRotation = Quaternion.identity;
            transform.localPosition = targetLocalPosition;
            transform.localRotation = Quaternion.identity;
            ResetVisualOffset();
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
            var previousMove = deployMoveDuration;
            var previousFlip = flipDuration;
            deployMoveDuration = moveDuration;
            this.flipDuration = flipDuration;
            try
            {
                await ApplyPlacement(
                    null,
                    targetLocalPosition,
                    Quaternion.identity,
                    CardBoardPhase.BattlefieldFaceUp,
                    animate: true);
            }
            finally
            {
                deployMoveDuration = previousMove;
                this.flipDuration = previousFlip;
            }
        }

        public UniTask SnapReserveOnAnchorAsync(Vector3 localPosition)
        {
            return ApplyPlacement(null, localPosition, Quaternion.identity, CardBoardPhase.ReserveFaceDown, animate: false);
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

        private async UniTask ApplyReservePlacementAsync(
            Vector3 targetLocalPosition,
            Quaternion targetLocalRotation,
            bool animate)
        {
            if (!IsMotionValid)
            {
                return;
            }

            gameObject.SetActive(true);
            ResetVisualScale();

            if (animate && !IsAtTargetLocalPose(targetLocalPosition, targetLocalRotation))
            {
                var token = this.GetCancellationTokenOnDestroy();
                await AnimateLocalMoveAsync(transform.localPosition, targetLocalPosition, deployMoveDuration, token);
                if (!IsMotionValid || token.IsCancellationRequested)
                {
                    return;
                }
            }

            FinalizeLocalPose(targetLocalPosition, targetLocalRotation);
            SetPhase(CardBoardPhase.ReserveFaceDown);
        }

        private async UniTask ApplyBattlefieldPlacementAsync(
            Vector3 targetLocalPosition,
            Quaternion targetLocalRotation,
            bool animate)
        {
            if (!IsMotionValid)
            {
                return;
            }

            var isDeploy = phase != CardBoardPhase.BattlefieldFaceUp;

            if (isDeploy && animate)
            {
                await AnimateDeployAsync(targetLocalPosition, targetLocalRotation);
                return;
            }

            if (animate && !IsAtTargetLocalPose(targetLocalPosition, targetLocalRotation))
            {
                await AnimateRealignAsync(targetLocalPosition, targetLocalRotation);
                return;
            }

            FinalizeBattlefieldPose(targetLocalPosition, targetLocalRotation);
        }

        private async UniTask AnimateDeployAsync(Vector3 targetLocalPosition, Quaternion targetLocalRotation)
        {
            var token = this.GetCancellationTokenOnDestroy();
            gameObject.SetActive(true);
            ResetVisualScale();
            transform.localRotation = targetLocalRotation;
            SetFaceDownInstant();
            SetFrontLabelsVisible(false);

            var fromReserve = phase == CardBoardPhase.ReserveFaceDown;
            UniTask moveTask;
            if (fromReserve && transform.parent != null)
            {
                var startWorld = transform.position;
                var targetWorld = transform.parent.TransformPoint(targetLocalPosition);
                moveTask = AnimateWorldMoveAsync(startWorld, targetWorld, deployMoveDuration, token);
            }
            else
            {
                moveTask = AnimateLocalMoveAsync(transform.localPosition, targetLocalPosition, deployMoveDuration, token);
            }

            var flipTask = AnimateFlipFaceUpAsync(flipDuration, token);
            await UniTask.WhenAll(moveTask, flipTask);

            if (!IsMotionValid || token.IsCancellationRequested)
            {
                return;
            }

            FinalizeBattlefieldPose(targetLocalPosition, targetLocalRotation);
        }

        private async UniTask AnimateRealignAsync(Vector3 targetLocalPosition, Quaternion targetLocalRotation)
        {
            if (IsAtTargetLocalPose(targetLocalPosition, targetLocalRotation))
            {
                return;
            }

            var token = this.GetCancellationTokenOnDestroy();
            await AnimateLocalMoveAsync(transform.localPosition, targetLocalPosition, deployMoveDuration, token);

            if (!IsMotionValid || token.IsCancellationRequested)
            {
                return;
            }

            FinalizeBattlefieldPose(targetLocalPosition, targetLocalRotation);
        }

        private void FinalizeBattlefieldPose(Vector3 targetLocalPosition, Quaternion targetLocalRotation)
        {
            FinalizeLocalPose(targetLocalPosition, targetLocalRotation);
            SetPhase(CardBoardPhase.BattlefieldFaceUp);
            RefreshHpInstant();
        }

        private void FinalizeLocalPose(Vector3 targetLocalPosition, Quaternion targetLocalRotation)
        {
            homeLocalPosition = targetLocalPosition;
            homeLocalRotation = targetLocalRotation;
            transform.localPosition = targetLocalPosition;
            transform.localRotation = targetLocalRotation;
            ResetVisualOffset();
        }

        private void ReparentPreserveWorld(Transform anchorParent)
        {
            if (anchorParent == null || transform.parent == anchorParent)
            {
                return;
            }

            transform.SetParent(anchorParent, worldPositionStays: true);
        }

        private bool IsAtTargetLocalPose(Vector3 localPosition, Quaternion localRotation)
        {
            return Vector3.Distance(transform.localPosition, localPosition) < 0.001f
                && Quaternion.Angle(transform.localRotation, localRotation) < 0.1f;
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
                transform.localPosition = from;
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

        private async UniTask AnimateWorldMoveAsync(
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
                transform.position = to;
                return;
            }

            if (useDotween)
            {
                transform.position = from;
                var tween = transform.DOMove(to, duration).SetEase(Ease.OutCubic);
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
                transform.position = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                await UniTask.Yield(token);
            }

            if (IsMotionValid)
            {
                transform.position = to;
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
            var duration = hpChangeDuration > 0f ? hpChangeDuration : 0.35f;
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

            displayHp = toHp;
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
    }
}
