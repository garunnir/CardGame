using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace CardGame.CardBattle.Presentation
{
    public static class BattleStatFloatingTextPresenter
    {
        private const float FloatDuration = 0.75f;
        private const float RiseDistance = 0.55f;
        private static readonly Color HealColor = new Color(0.45f, 1f, 0.55f, 1f);

        public static async UniTask ShowAsync(
            IPresentationTargetView target,
            StatFeedbackKind kind,
            int amount,
            CancellationToken cancellationToken = default)
        {
            if (target?.ViewTransform == null || amount <= 0 || kind != StatFeedbackKind.Heal)
            {
                return;
            }

            var anchor = target.ViewTransform;
            var go = new GameObject("BattleStatFloat");
            go.transform.SetPositionAndRotation(
                anchor.position + Vector3.up * 0.25f,
                Quaternion.identity);

            var text = go.AddComponent<TextMeshPro>();
            text.text = $"+{amount}";
            text.fontSize = 4f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = HealColor;
            text.sortingOrder = 100;

            var endPosition = go.transform.position + Vector3.up * RiseDistance;
            var moveTween = go.transform.DOMove(endPosition, FloatDuration).SetEase(Ease.OutQuad);
            var endColor = new Color(HealColor.r, HealColor.g, HealColor.b, 0f);
            var fadeTween = DOTween.To(
                () => text.color,
                value => text.color = value,
                endColor,
                FloatDuration * 0.35f)
                .SetDelay(FloatDuration * 0.65f);

            try
            {
                await UniTask.WhenAll(
                    AwaitTweenAsync(moveTween, cancellationToken),
                    AwaitTweenAsync(fadeTween, cancellationToken));
            }
            finally
            {
                if (go != null)
                {
                    Object.Destroy(go);
                }
            }
        }

        private static UniTask AwaitTweenAsync(Tween tween, CancellationToken cancellationToken)
        {
            if (tween == null || !tween.IsActive())
            {
                return UniTask.CompletedTask;
            }

            var tcs = new UniTaskCompletionSource();
            tween.OnComplete(() => tcs.TrySetResult());
            tween.OnKill(() => tcs.TrySetResult());

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(() =>
                {
                    if (tween.IsActive())
                    {
                        tween.Kill();
                    }
                });
            }

            return tcs.Task;
        }
    }
}
