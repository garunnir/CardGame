using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace CardGame.CardBattle.Cards
{
    internal sealed class HeroCombatMotion
    {
        public const float DefaultTweenDuration = 0.35f;

        private readonly IHeroMotionHost host;
        private CancellationTokenSource statAnimationCts;

        public HeroCombatMotion(IHeroMotionHost host)
        {
            this.host = host;
        }

        public void Dispose()
        {
            statAnimationCts?.Cancel();
            statAnimationCts?.Dispose();
            statAnimationCts = null;
        }

        public void PlayHpChange(int fromHp, int toHp, Action onComplete = null)
        {
            if (!host.IsActiveAndEnabled)
            {
                host.RefreshStatsInstant();
                onComplete?.Invoke();
                return;
            }

            statAnimationCts?.Cancel();
            statAnimationCts?.Dispose();
            statAnimationCts = new CancellationTokenSource();
            RunHpChangeAsync(fromHp, toHp, statAnimationCts.Token, onComplete).Forget();
        }

        public UniTask TweenHpShieldAsync(
            int fromHp,
            int toHp,
            int fromShield,
            int toShield,
            int maxHp,
            float duration)
        {
            statAnimationCts?.Cancel();
            statAnimationCts?.Dispose();
            statAnimationCts = new CancellationTokenSource();
            return RunHpShieldTweenAsync(
                fromHp,
                toHp,
                fromShield,
                toShield,
                maxHp,
                duration,
                statAnimationCts.Token);
        }

        public UniTask TweenMpAsync(int fromMp, int toMp, int maxMp, float duration)
        {
            statAnimationCts?.Cancel();
            statAnimationCts?.Dispose();
            statAnimationCts = new CancellationTokenSource();
            return RunMpTweenAsync(fromMp, toMp, maxMp, duration, statAnimationCts.Token);
        }

        public void PlayAttackDash(
            Vector3 worldTarget,
            float dashDuration,
            Action onImpact,
            Action onComplete = null)
        {
            if (host.ShakeRoot == null)
            {
                onImpact?.Invoke();
                onComplete?.Invoke();
                return;
            }

            var duration = dashDuration > 0f ? dashDuration : host.AttackDashDuration;
            var half = duration * 0.5f;
            var direction = (worldTarget - host.Transform.position).normalized;
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = host.Transform.forward;
            }

            var dashOffset = host.Transform.InverseTransformDirection(direction) * host.AttackDashDistance;

            if (host.UseDotween)
            {
                var seq = DOTween.Sequence();
                seq.Append(host.ShakeRoot.DOLocalMove(dashOffset, half).SetEase(Ease.OutQuad));
                seq.AppendCallback(() => onImpact?.Invoke());
                seq.Append(host.ShakeRoot.DOLocalMove(Vector3.zero, half).SetEase(Ease.InQuad));
                seq.OnComplete(() => onComplete?.Invoke());
            }
            else
            {
                RunAttackDashAsync(dashOffset, half, onImpact, onComplete, host.GetDestroyCancellationToken()).Forget();
            }
        }

        public void PlayHitShake(float strength, Action onComplete = null)
        {
            var shakeStrength = strength > 0f
                ? new Vector3(strength * 0.05f, strength * 0.035f, 0f)
                : new Vector3(0.08f, 0.05f, 0f);

            if (host.ShakeRoot == null)
            {
                onComplete?.Invoke();
                return;
            }

            if (host.UseDotween)
            {
                host.ShakeRoot.DOShakePosition(0.25f, shakeStrength, 20, 90f, false, true)
                    .OnComplete(() =>
                    {
                        ResetVisualOffset();
                        onComplete?.Invoke();
                    });
            }
            else
            {
                RunHitShakeAsync(onComplete, host.GetDestroyCancellationToken()).Forget();
            }
        }

        private async UniTaskVoid RunHpChangeAsync(
            int fromHp,
            int toHp,
            CancellationToken token,
            Action onComplete)
        {
            try
            {
                await AnimateHpAsync(fromHp, toHp, token);
                onComplete?.Invoke();
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async UniTask RunHpShieldTweenAsync(
            int fromHp,
            int toHp,
            int fromShield,
            int toShield,
            int maxHp,
            float duration,
            CancellationToken token)
        {
            if (fromHp == toHp && fromShield == toShield)
            {
                host.SetStatsVisual(toHp, toShield, host.DisplayMp);
                return;
            }

            var tweenDuration = duration > 0f ? duration : DefaultTweenDuration;
            var elapsed = 0f;
            while (elapsed < tweenDuration)
            {
                token.ThrowIfCancellationRequested();
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / tweenDuration);
                var hp = Mathf.RoundToInt(Mathf.Lerp(fromHp, toHp, t));
                var shield = Mathf.RoundToInt(Mathf.Lerp(fromShield, toShield, t));
                host.SetStatsVisual(hp, shield, host.DisplayMp);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            host.DisplayHp = toHp;
            host.DisplayShield = toShield;
            host.SetStatsVisual(toHp, toShield, host.DisplayMp);
        }

        private async UniTask RunMpTweenAsync(
            int fromMp,
            int toMp,
            int maxMp,
            float duration,
            CancellationToken token)
        {
            if (fromMp == toMp)
            {
                host.SetStatsVisual(host.DisplayHp, host.DisplayShield, toMp);
                return;
            }

            var tweenDuration = duration > 0f ? duration : DefaultTweenDuration;
            var elapsed = 0f;
            while (elapsed < tweenDuration)
            {
                token.ThrowIfCancellationRequested();
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / tweenDuration);
                var mp = Mathf.RoundToInt(Mathf.Lerp(fromMp, toMp, t));
                host.SetStatsVisual(host.DisplayHp, host.DisplayShield, mp);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            host.DisplayMp = toMp;
            host.SetStatsVisual(host.DisplayHp, host.DisplayShield, toMp);
        }

        private async UniTaskVoid RunAttackDashAsync(
            Vector3 dashOffset,
            float halfDuration,
            Action onImpact,
            Action onComplete,
            CancellationToken token)
        {
            await AnimateVisualOffsetAsync(Vector3.zero, dashOffset, halfDuration, token);
            onImpact?.Invoke();
            await AnimateVisualOffsetAsync(dashOffset, Vector3.zero, halfDuration, token);
            onComplete?.Invoke();
        }

        private async UniTaskVoid RunHitShakeAsync(Action onComplete, CancellationToken token)
        {
            const int shakes = 6;
            for (var i = 0; i < shakes; i++)
            {
                token.ThrowIfCancellationRequested();
                host.ShakeRoot.localPosition = new Vector3(
                    UnityEngine.Random.Range(-0.04f, 0.04f),
                    UnityEngine.Random.Range(-0.03f, 0.03f),
                    0f);
                await UniTask.Delay(TimeSpan.FromSeconds(0.03f), cancellationToken: token);
            }

            ResetVisualOffset();
            onComplete?.Invoke();
        }

        private async UniTask AnimateHpAsync(int fromHp, int toHp, CancellationToken token)
        {
            var duration = host.HpChangeDuration > 0f ? host.HpChangeDuration : DefaultTweenDuration;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                token.ThrowIfCancellationRequested();
                elapsed += Time.deltaTime;
                var value = Mathf.RoundToInt(Mathf.Lerp(fromHp, toHp, Mathf.Clamp01(elapsed / duration)));
                host.SetStatsVisual(value, host.DisplayShield, host.DisplayMp);
                await UniTask.Yield(token);
            }

            host.DisplayHp = toHp;
            host.SetStatsVisual(toHp, host.DisplayShield, host.DisplayMp);
        }

        private async UniTask AnimateVisualOffsetAsync(
            Vector3 from,
            Vector3 to,
            float duration,
            CancellationToken token)
        {
            if (host.ShakeRoot == null)
            {
                return;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                token.ThrowIfCancellationRequested();
                elapsed += Time.deltaTime;
                host.ShakeRoot.localPosition = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                await UniTask.Yield(token);
            }

            host.ShakeRoot.localPosition = to;
        }

        private void ResetVisualOffset()
        {
            if (host.ShakeRoot != null && host.ShakeRoot != host.Transform)
            {
                host.ShakeRoot.localPosition = Vector3.zero;
            }
        }
    }
}
