using System.Threading;
using CardGame.CardBattle.Cards;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace CardGame.CardBattle.Presentation
{
    public static class BattleStatFloatingTextPresenter
    {
        private const string OverlayShaderName = "TextMeshPro/Distance Field Overlay";

        private static Shader cachedOverlayShader;
        private static bool overlayShaderResolved;

        public static async UniTask ShowAsync(
            StatFloatingTextPresentationAsset settings,
            IPresentationTargetView target,
            StatFeedbackKind kind,
            int amount,
            CancellationToken cancellationToken = default)
        {
            if (settings == null || target?.ViewTransform == null || amount <= 0)
            {
                return;
            }

            if (!TryGetDisplay(settings, kind, amount, out var label, out var color))
            {
                return;
            }

            var targetRoot = target.ViewTransform;
            if (!TryResolvePose(settings, targetRoot, out var spawnPosition, out var rotation, out var riseDirection))
            {
                return;
            }

            var go = new GameObject("BattleStatFloat");
            go.transform.SetPositionAndRotation(spawnPosition, rotation);

            var text = go.AddComponent<TextMeshPro>();
            text.text = label;
            text.fontSize = settings.fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            ConfigureTextRenderer(text, settings, targetRoot);

            var endPosition = spawnPosition + riseDirection * settings.riseDistance;
            var moveTween = go.transform.DOMove(endPosition, settings.duration).SetEase(Ease.OutQuad);
            var endColor = new Color(color.r, color.g, color.b, 0f);
            var fadeDelay = settings.duration * settings.fadeStartRatio;
            var fadeDuration = settings.duration * settings.fadeDurationRatio;
            var fadeTween = DOTween.To(
                () => text.color,
                value => text.color = value,
                endColor,
                fadeDuration)
                .SetDelay(fadeDelay);

            try
            {
                await UniTask.WhenAll(
                    PresentationTweenAwaiter.AwaitAsync(moveTween, cancellationToken),
                    PresentationTweenAwaiter.AwaitAsync(fadeTween, cancellationToken));
            }
            finally
            {
                if (go != null)
                {
                    Object.Destroy(go);
                }
            }
        }

        private static void ConfigureTextRenderer(
            TextMeshPro text,
            StatFloatingTextPresentationAsset settings,
            Transform targetRoot)
        {
            text.sortingOrder = ResolveSortingOrder(settings, targetRoot);

            if (!settings.renderAboveCardFace)
            {
                return;
            }

            var overlayShader = ResolveOverlayShader();
            if (overlayShader == null)
            {
                return;
            }

            text.fontMaterial.shader = overlayShader;
        }

        private static int ResolveSortingOrder(
            StatFloatingTextPresentationAsset settings,
            Transform targetRoot)
        {
            var baseline = targetRoot.GetComponent<HeroEntity>() != null
                ? HeroVisualSorting.FloatingText
                : CardFaceView.FloatingTextSortingOrder;

            return settings.sortingOrder > 0
                ? Mathf.Max(settings.sortingOrder, baseline)
                : baseline;
        }

        private static bool TryResolvePose(
            StatFloatingTextPresentationAsset settings,
            Transform targetRoot,
            out Vector3 spawnPosition,
            out Quaternion rotation,
            out Vector3 riseDirection)
        {
            spawnPosition = default;
            rotation = Quaternion.identity;
            riseDirection = Vector3.up;

            if (targetRoot == null)
            {
                return false;
            }

            var reference = ResolveReferenceTransform(settings, targetRoot);
            if (reference == null)
            {
                return false;
            }

            spawnPosition = reference.position
                + reference.TransformVector(settings.spawnOffsetLocal)
                + reference.forward * settings.faceForwardOffset;
            rotation = ResolveRotation(settings, reference);
            riseDirection = reference.TransformDirection(settings.riseDirectionLocal.normalized);
            if (riseDirection.sqrMagnitude < 0.0001f)
            {
                riseDirection = reference.up;
            }

            return true;
        }

        private static Transform ResolveReferenceTransform(
            StatFloatingTextPresentationAsset settings,
            Transform targetRoot)
        {
            switch (settings.orientationMode)
            {
                case StatFloatingTextOrientationMode.FollowHpLabel:
                    return FindHpLabelTransform(targetRoot) ?? targetRoot;
                case StatFloatingTextOrientationMode.FollowTargetRoot:
                case StatFloatingTextOrientationMode.WorldEuler:
                    return targetRoot;
                default:
                    return targetRoot;
            }
        }

        private static Quaternion ResolveRotation(
            StatFloatingTextPresentationAsset settings,
            Transform reference)
        {
            if (settings.orientationMode == StatFloatingTextOrientationMode.WorldEuler)
            {
                return Quaternion.Euler(settings.rotationEuler);
            }

            return reference.rotation * Quaternion.Euler(settings.rotationEuler);
        }

        private static Transform FindHpLabelTransform(Transform root)
        {
            if (root == null)
            {
                return null;
            }

            if (root.TryGetComponent<CardEntity>(out _))
            {
                return root.GetComponent<ICardMotionHost>()?.HpLabel?.transform;
            }

            if (root.TryGetComponent<HeroEntity>(out _))
            {
                return root.GetComponent<IHeroMotionHost>()?.HpLabel?.transform;
            }

            return null;
        }

        private static Shader ResolveOverlayShader()
        {
            if (!overlayShaderResolved)
            {
                cachedOverlayShader = Shader.Find(OverlayShaderName);
                overlayShaderResolved = true;
            }

            return cachedOverlayShader;
        }

        private static bool TryGetDisplay(
            StatFloatingTextPresentationAsset settings,
            StatFeedbackKind kind,
            int amount,
            out string label,
            out Color color)
        {
            switch (kind)
            {
                case StatFeedbackKind.Heal:
                    label = $"+{amount}";
                    color = settings.healColor;
                    return true;
                case StatFeedbackKind.Damage:
                    label = $"-{amount}";
                    color = settings.damageColor;
                    return true;
                default:
                    label = null;
                    color = default;
                    return false;
            }
        }

    }
}
