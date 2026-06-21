using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace CardGame.CardBattle.Presentation
{
    internal static class PresentationTweenAwaiter
    {
        public static UniTask AwaitAsync(Tween tween, CancellationToken cancellationToken = default)
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
