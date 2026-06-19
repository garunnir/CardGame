using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace CardGame.CardBattle.Cards
{
    internal sealed class CardBoardMotion
    {
        private readonly ICardMotionHost host;

        public CardBoardMotion(ICardMotionHost host)
        {
            this.host = host;
        }

        public void CancelMotion()
        {
            host.CancelTransformMotion();
        }

        public async UniTask ApplyPlacement(
            Transform anchorParent,
            Vector3 targetLocalPosition,
            Quaternion targetLocalRotation,
            CardBoardPhase targetPhase,
            bool animate)
        {
            if (!host.IsMotionValid)
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

        private async UniTask ApplyReservePlacementAsync(
            Vector3 targetLocalPosition,
            Quaternion targetLocalRotation,
            bool animate)
        {
            if (!host.IsMotionValid)
            {
                return;
            }

            host.GameObject.SetActive(true);
            ResetVisualScale();

            if (animate && !IsAtTargetLocalPose(targetLocalPosition, targetLocalRotation))
            {
                var token = host.GetDestroyCancellationToken();
                await AnimateLocalMoveAsync(host.Transform.localPosition, targetLocalPosition, host.DeployMoveDuration, token);
                if (!host.IsMotionValid || token.IsCancellationRequested)
                {
                    return;
                }
            }

            FinalizeLocalPose(targetLocalPosition, targetLocalRotation);
            host.SetPhase(CardBoardPhase.ReserveFaceDown);
        }

        private async UniTask ApplyBattlefieldPlacementAsync(
            Vector3 targetLocalPosition,
            Quaternion targetLocalRotation,
            bool animate)
        {
            if (!host.IsMotionValid)
            {
                return;
            }

            var isDeploy = host.Phase != CardBoardPhase.BattlefieldFaceUp;

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
            var token = host.GetDestroyCancellationToken();
            host.GameObject.SetActive(true);
            ResetVisualScale();
            host.Transform.localRotation = targetLocalRotation;
            host.SetFaceDownInstant();
            host.SetFrontLabelsVisible(false);

            var fromReserve = host.Phase == CardBoardPhase.ReserveFaceDown;
            UniTask moveTask;
            if (fromReserve && host.Transform.parent != null)
            {
                var startWorld = host.Transform.position;
                var targetWorld = host.Transform.parent.TransformPoint(targetLocalPosition);
                moveTask = AnimateWorldMoveAsync(startWorld, targetWorld, host.DeployMoveDuration, token);
            }
            else
            {
                moveTask = AnimateLocalMoveAsync(
                    host.Transform.localPosition,
                    targetLocalPosition,
                    host.DeployMoveDuration,
                    token);
            }

            var flipTask = AnimateFlipFaceUpAsync(host.FlipDuration, token);
            await UniTask.WhenAll(moveTask, flipTask);

            if (!host.IsMotionValid || token.IsCancellationRequested)
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

            var token = host.GetDestroyCancellationToken();
            await AnimateLocalMoveAsync(
                host.Transform.localPosition,
                targetLocalPosition,
                host.DeployMoveDuration,
                token);

            if (!host.IsMotionValid || token.IsCancellationRequested)
            {
                return;
            }

            FinalizeBattlefieldPose(targetLocalPosition, targetLocalRotation);
        }

        private void FinalizeBattlefieldPose(Vector3 targetLocalPosition, Quaternion targetLocalRotation)
        {
            FinalizeLocalPose(targetLocalPosition, targetLocalRotation);
            host.SetPhase(CardBoardPhase.BattlefieldFaceUp);
            host.RefreshHpInstant();
        }

        private void FinalizeLocalPose(Vector3 targetLocalPosition, Quaternion targetLocalRotation)
        {
            host.HomeLocalPosition = targetLocalPosition;
            host.HomeLocalRotation = targetLocalRotation;
            host.Transform.localPosition = targetLocalPosition;
            host.Transform.localRotation = targetLocalRotation;
            ResetVisualOffset();
        }

        private void ReparentPreserveWorld(Transform anchorParent)
        {
            if (anchorParent == null || host.Transform.parent == anchorParent)
            {
                return;
            }

            host.Transform.SetParent(anchorParent, worldPositionStays: true);
        }

        private bool IsAtTargetLocalPose(Vector3 localPosition, Quaternion localRotation)
        {
            return Vector3.Distance(host.Transform.localPosition, localPosition) < 0.001f
                && Quaternion.Angle(host.Transform.localRotation, localRotation) < 0.1f;
        }

        private async UniTask AnimateLocalMoveAsync(
            Vector3 from,
            Vector3 to,
            float duration,
            CancellationToken token)
        {
            if (!host.IsMotionValid)
            {
                return;
            }

            if (duration <= 0f)
            {
                host.Transform.localPosition = to;
                return;
            }

            if (host.UseDotween)
            {
                host.Transform.localPosition = from;
                var tween = host.Transform.DOLocalMove(to, duration).SetEase(Ease.OutCubic);
                await UniTask.WaitUntil(() => !tween.IsActive(), cancellationToken: token);
                return;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                if (!host.IsMotionValid || token.IsCancellationRequested)
                {
                    return;
                }

                elapsed += Time.deltaTime;
                host.Transform.localPosition = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                await UniTask.Yield(token);
            }

            if (host.IsMotionValid)
            {
                host.Transform.localPosition = to;
            }
        }

        private async UniTask AnimateWorldMoveAsync(
            Vector3 from,
            Vector3 to,
            float duration,
            CancellationToken token)
        {
            if (!host.IsMotionValid)
            {
                return;
            }

            if (duration <= 0f)
            {
                host.Transform.position = to;
                return;
            }

            if (host.UseDotween)
            {
                host.Transform.position = from;
                var tween = host.Transform.DOMove(to, duration).SetEase(Ease.OutCubic);
                await UniTask.WaitUntil(() => !tween.IsActive(), cancellationToken: token);
                return;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                if (!host.IsMotionValid || token.IsCancellationRequested)
                {
                    return;
                }

                elapsed += Time.deltaTime;
                host.Transform.position = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                await UniTask.Yield(token);
            }

            if (host.IsMotionValid)
            {
                host.Transform.position = to;
            }
        }

        private async UniTask AnimateFlipFaceUpAsync(float duration, CancellationToken token)
        {
            if (!host.IsMotionValid)
            {
                return;
            }

            if (duration <= 0f)
            {
                host.SetFaceUpInstant();
                return;
            }

            const float startY = 180f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                if (!host.IsMotionValid || token.IsCancellationRequested)
                {
                    return;
                }

                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var y = Mathf.Lerp(startY, 0f, t);
                host.ShakeRoot.localRotation = Quaternion.Euler(0f, y, 0f);
                await UniTask.Yield(token);
            }

            host.SetFaceUpInstant();
        }

        private void ResetVisualOffset()
        {
            if (host.ShakeRoot != null && host.ShakeRoot != host.Transform)
            {
                host.ShakeRoot.localPosition = Vector3.zero;
            }
        }

        private void ResetVisualScale()
        {
            if (host.ShakeRoot != null)
            {
                host.ShakeRoot.localScale = Vector3.one;
            }
        }
    }
}
