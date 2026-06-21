using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace CardGame.CardBattle.Presentation
{
    public static class PresentationProjectileFlight
    {
        private const float MinDuration = 0.15f;
        private const float DefaultArcHeight = 0.5f;

        public static async UniTask FlyAsync(
            Transform from,
            Transform to,
            GameObject prefab,
            float duration,
            ProjectilePathKind pathKind,
            float arcHeight,
            CancellationToken cancellationToken = default)
        {
            if (from == null || to == null || prefab == null)
            {
                return;
            }

            var flightDuration = duration > 0f ? duration : MinDuration;
            var instance = Object.Instantiate(prefab, from.position, from.rotation);
            var transform = instance.transform;

            try
            {
                var targetPosition = to.position;
                FaceTarget(transform, targetPosition);

                if (pathKind == ProjectilePathKind.Arc)
                {
                    var height = arcHeight > 0f ? arcHeight : DefaultArcHeight;
                    var mid = Vector3.Lerp(from.position, targetPosition, 0.5f) + Vector3.up * height;
                    var path = new[] { from.position, mid, targetPosition };
                    var tween = transform.DOPath(path, flightDuration, PathType.CatmullRom)
                        .SetEase(Ease.Linear);
                    await PresentationTweenAwaiter.AwaitAsync(tween, cancellationToken);
                }
                else
                {
                    var tween = transform.DOMove(targetPosition, flightDuration).SetEase(Ease.Linear);
                    await PresentationTweenAwaiter.AwaitAsync(tween, cancellationToken);
                }
            }
            finally
            {
                if (instance != null)
                {
                    Object.Destroy(instance);
                }
            }
        }

        private static void FaceTarget(Transform projectile, Vector3 targetPosition)
        {
            var direction = targetPosition - projectile.position;
            if (direction.sqrMagnitude < 0.001f)
            {
                return;
            }

            projectile.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

    }
}
