using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardGame.CardBattle.Presentation
{
    internal static class PresentationCueMotionBridge
    {
        public static async UniTask PlayAttackDashAsync(BattlePresentationSpec spec, float dashDuration)
        {
            if (spec.Kind == PresentationKind.HeroStrike)
            {
                var strikerView = spec.GetHeroTargetView(spec.StrikerHero);
                var defenderView = spec.GetHeroTargetView(spec.DefenderHero);
                if (strikerView == null || defenderView == null)
                {
                    return;
                }

                await PresentationAsyncBridge.FromCallback(onComplete =>
                    strikerView.PlayAttackDash(
                        defenderView.ViewTransform.position,
                        dashDuration,
                        null,
                        onComplete));
                return;
            }

            var attackerView = spec.GetCardTargetView(spec.AttackerCard.InstanceId);
            IPresentationTargetView targetView = spec.Kind == PresentationKind.CardVsHero
                ? spec.GetHeroTargetView(spec.PrimaryTargetHero)
                : spec.GetCardTargetView(spec.PrimaryTargetCard.InstanceId);

            if (attackerView == null || targetView == null)
            {
                return;
            }

            await PresentationAsyncBridge.FromCallback(onComplete =>
                attackerView.PlayAttackDash(
                    targetView.ViewTransform.position,
                    dashDuration,
                    null,
                    onComplete));
        }

        public static UniTask PlayHitShakeAsync(IPresentationTargetView view, float strength)
        {
            if (view == null)
            {
                return UniTask.CompletedTask;
            }

            return PresentationAsyncBridge.FromCallback(onComplete =>
                view.PlayHitShake(strength, onComplete));
        }

        public static UniTask PlayHpBarTweenAsync(ICardBattleView view, int fromHp, int toHp)
        {
            if (view == null || fromHp < 0 || toHp < 0)
            {
                return UniTask.CompletedTask;
            }

            return PresentationAsyncBridge.FromCallback(onComplete =>
                view.PlayHpChange(fromHp, toHp, onComplete));
        }

        public static async UniTask PlayDeathPresentationAsync(BattlePresentationSpec spec, CardInstanceId subjectId)
        {
            if (!subjectId.IsValid || !spec.CardViewRegistry.TryGetModel(subjectId, out var subject))
            {
                return;
            }

            var view = spec.GetCardView(subjectId);
            spec.Presentation?.PlayDeath(subject, view);
            if (view == null)
            {
                return;
            }

            await PresentationAsyncBridge.FromCallback(onComplete =>
                view.PlayDeathVisual(onComplete));
        }
    }
}
