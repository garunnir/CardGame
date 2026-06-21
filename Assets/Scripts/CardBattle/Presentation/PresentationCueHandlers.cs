using System;
using System.Collections.Generic;
using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardGame.CardBattle.Presentation
{
    internal interface IPresentationCueHandler
    {
        PresentationCueKind Kind { get; }
        bool SupportsBattle { get; }
        bool SupportsTurnStart { get; }
        UniTask ExecuteAsync(PresentationCueContext context, PresentationCue cue);
    }

    internal static class PresentationCueDispatcher
    {
        private static readonly Dictionary<PresentationCueKind, IPresentationCueHandler> battleHandlers;
        private static readonly Dictionary<PresentationCueKind, IPresentationCueHandler> turnHandlers;

        static PresentationCueDispatcher()
        {
            battleHandlers = new Dictionary<PresentationCueKind, IPresentationCueHandler>();
            turnHandlers = new Dictionary<PresentationCueKind, IPresentationCueHandler>();

            Register(new WaitCueHandler());
            Register(new WaitBeforeSecondaryCueHandler());
            Register(new FlyProjectileCueHandler());
            Register(new PlayProjectileImpactCueHandler());
            Register(new PlayStatFloatingTextCueHandler());
            Register(new HpBarTweenCueHandler());
            Register(new HeroStatTweenCueHandler());
            Register(new UiHealerBloomCueHandler());
            Register(new PlayHeroSupportFromSlotCueHandler());
            Register(new UiAttackBloomCueHandler());
            Register(new PlayAttackPresentationCueHandler());
            Register(new AttackDashCueHandler());
            Register(new PlayOnHitPresentationCueHandler());
            Register(new PlayReceivedHitPresentationCueHandler());
            Register(new PlayHeroReceivedHitPresentationCueHandler());
            Register(new HitShakeCueHandler());
            Register(new PlayCounterOnHitPresentationCueHandler());
            Register(new PlaySecondaryOnHitPresentationCueHandler());
            Register(new CameraShakeCueHandler());
            Register(new PlayDeathPresentationCueHandler());
            Register(new PlayHeroStrikePresentationCueHandler());
            Register(new PlayHeroShieldBuffPresentationCueHandler());
        }

        public static UniTask ExecuteBattleAsync(PresentationCueContext context, PresentationCue cue)
        {
            if (battleHandlers.TryGetValue(cue.Kind, out var handler))
            {
                return handler.ExecuteAsync(context, cue);
            }

            return UniTask.CompletedTask;
        }

        public static UniTask ExecuteTurnStartAsync(PresentationCueContext context, PresentationCue cue)
        {
            if (turnHandlers.TryGetValue(cue.Kind, out var handler))
            {
                return handler.ExecuteAsync(context, cue);
            }

            return UniTask.CompletedTask;
        }

        public static UniTask ExecuteFlyProjectileAsync(PresentationCueContext context, PresentationCue cue)
        {
            return SharedHandlers.FlyProjectile.ExecuteAsync(context, cue);
        }

        private static class SharedHandlers
        {
            internal static readonly FlyProjectileCueHandler FlyProjectile = new FlyProjectileCueHandler();
        }

        private static void Register(IPresentationCueHandler handler)
        {
            if (handler.SupportsBattle)
            {
                battleHandlers[handler.Kind] = handler;
            }

            if (handler.SupportsTurnStart)
            {
                turnHandlers[handler.Kind] = handler;
            }
        }
    }

    internal sealed class WaitCueHandler : IPresentationCueHandler
    {
        public PresentationCueKind Kind => PresentationCueKind.Wait;
        public bool SupportsBattle => true;
        public bool SupportsTurnStart => true;

        public UniTask ExecuteAsync(PresentationCueContext context, PresentationCue cue)
        {
            return UniTask.Delay(TimeSpan.FromSeconds(cue.Duration));
        }
    }

    internal sealed class FlyProjectileCueHandler : IPresentationCueHandler
    {
        public PresentationCueKind Kind => PresentationCueKind.FlyProjectile;
        public bool SupportsBattle => true;
        public bool SupportsTurnStart => true;

        public async UniTask ExecuteAsync(PresentationCueContext context, PresentationCue cue)
        {
            if (context.Presentation == null
                || cue.ProjectilePresentation == null
                || !cue.SourceId.IsValid)
            {
                return;
            }

            var fromView = context.ResolveCardTargetView(cue.SourceId);
            var toView = context.ResolveCueTargetView(cue);
            if (fromView == null || toView == null)
            {
                return;
            }

            await context.Presentation.FlyProjectileAsync(cue.ProjectilePresentation, fromView, toView);
        }
    }

    internal sealed class PlayProjectileImpactCueHandler : IPresentationCueHandler
    {
        public PresentationCueKind Kind => PresentationCueKind.PlayProjectileImpact;
        public bool SupportsBattle => true;
        public bool SupportsTurnStart => true;

        public UniTask ExecuteAsync(PresentationCueContext context, PresentationCue cue)
        {
            if (context.Presentation == null)
            {
                return UniTask.CompletedTask;
            }

            var targetView = context.ResolveCueTargetView(cue);
            context.Presentation.PlayProjectileImpact(cue.ProjectilePresentation, targetView);
            return UniTask.CompletedTask;
        }
    }

    internal sealed class PlayStatFloatingTextCueHandler : IPresentationCueHandler
    {
        public PresentationCueKind Kind => PresentationCueKind.PlayStatFloatingText;
        public bool SupportsBattle => true;
        public bool SupportsTurnStart => true;

        public async UniTask ExecuteAsync(PresentationCueContext context, PresentationCue cue)
        {
            if (context.Presentation == null || cue.StatAmount <= 0)
            {
                return;
            }

            var targetView = context.ResolveCueTargetView(cue);
            if (targetView == null)
            {
                return;
            }

            await context.Presentation.ShowStatFloatingTextAsync(
                targetView,
                cue.StatFeedbackKind,
                cue.StatAmount);
        }
    }

    internal sealed class HpBarTweenCueHandler : IPresentationCueHandler
    {
        public PresentationCueKind Kind => PresentationCueKind.HpBarTween;
        public bool SupportsBattle => true;
        public bool SupportsTurnStart => true;

        public UniTask ExecuteAsync(PresentationCueContext context, PresentationCue cue)
        {
            if (context.IsBattle)
            {
                return PresentationCueMotionBridge.PlayHpBarTweenAsync(
                    context.Spec.GetCardView(cue.SubjectId),
                    cue.HpFrom,
                    cue.HpTo);
            }

            if (context.ViewRegistry != null && context.ViewRegistry.TryGetView(cue.SubjectId, out var healTarget))
            {
                healTarget.SetHpDisplay(cue.HpFrom);
                return PresentationCueMotionBridge.PlayHpBarTweenAsync(healTarget, cue.HpFrom, cue.HpTo);
            }

            return UniTask.CompletedTask;
        }
    }

    internal sealed class HeroStatTweenCueHandler : IPresentationCueHandler
    {
        public PresentationCueKind Kind => PresentationCueKind.HeroStatTween;
        public bool SupportsBattle => true;
        public bool SupportsTurnStart => true;

        public UniTask ExecuteAsync(PresentationCueContext context, PresentationCue cue)
        {
            if (context.HeroPresenter == null)
            {
                return UniTask.CompletedTask;
            }

            return context.HeroPresenter.PlayStatTweenAsync(
                cue.SubjectHeroId,
                cue.HpFrom,
                cue.HpTo,
                cue.ShieldFrom,
                cue.ShieldTo,
                cue.MpFrom,
                cue.MpTo);
        }
    }

    internal sealed class UiHealerBloomCueHandler : IPresentationCueHandler
    {
        public PresentationCueKind Kind => PresentationCueKind.UiHealerBloom;
        public bool SupportsBattle => false;
        public bool SupportsTurnStart => true;

        public UniTask ExecuteAsync(PresentationCueContext context, PresentationCue cue)
        {
            context.Ui?.PulseHealerBloom();
            return UniTask.CompletedTask;
        }
    }

    internal sealed class PlayHeroSupportFromSlotCueHandler : IPresentationCueHandler
    {
        public PresentationCueKind Kind => PresentationCueKind.PlayHeroSupportFromSlot;
        public bool SupportsBattle => false;
        public bool SupportsTurnStart => true;

        public UniTask ExecuteAsync(PresentationCueContext context, PresentationCue cue)
        {
            if (cue.IsMpGain)
            {
                context.Presentation?.PlayMpGainStub();
            }

            return UniTask.CompletedTask;
        }
    }

    internal sealed class UiAttackBloomCueHandler : IPresentationCueHandler
    {
        public PresentationCueKind Kind => PresentationCueKind.UiAttackBloom;
        public bool SupportsBattle => true;
        public bool SupportsTurnStart => false;

        public UniTask ExecuteAsync(PresentationCueContext context, PresentationCue cue)
        {
            context.Spec.Ui?.PulseAttackBloom();
            return UniTask.CompletedTask;
        }
    }

    internal sealed class PlayAttackPresentationCueHandler : IPresentationCueHandler
    {
        public PresentationCueKind Kind => PresentationCueKind.PlayAttackPresentation;
        public bool SupportsBattle => true;
        public bool SupportsTurnStart => false;

        public UniTask ExecuteAsync(PresentationCueContext context, PresentationCue cue)
        {
            context.Spec.Presentation?.PlayAttack(
                context.Spec.AttackerCard,
                context.Spec.GetCardView(context.Spec.AttackerCard.InstanceId));
            return UniTask.CompletedTask;
        }
    }

    internal sealed class AttackDashCueHandler : IPresentationCueHandler
    {
        public PresentationCueKind Kind => PresentationCueKind.AttackDash;
        public bool SupportsBattle => true;
        public bool SupportsTurnStart => false;

        public UniTask ExecuteAsync(PresentationCueContext context, PresentationCue cue)
        {
            return PresentationCueMotionBridge.PlayAttackDashAsync(context.Spec, cue.Duration);
        }
    }

    internal sealed class PlayOnHitPresentationCueHandler : IPresentationCueHandler
    {
        public PresentationCueKind Kind => PresentationCueKind.PlayOnHitPresentation;
        public bool SupportsBattle => true;
        public bool SupportsTurnStart => false;

        public UniTask ExecuteAsync(PresentationCueContext context, PresentationCue cue)
        {
            var spec = context.Spec;
            if (spec.Kind == PresentationKind.CardVsHero && spec.PrimaryTargetHero != null)
            {
                spec.Presentation?.PlayOnHitToHero(
                    spec.AttackerCard,
                    spec.GetHeroTargetView(spec.PrimaryTargetHero));
            }
            else if (spec.PrimaryTargetCard != null)
            {
                spec.Presentation?.PlayOnHit(
                    spec.AttackerCard,
                    spec.GetCardView(spec.PrimaryTargetCard.InstanceId));
            }

            return UniTask.CompletedTask;
        }
    }

    internal sealed class PlayReceivedHitPresentationCueHandler : IPresentationCueHandler
    {
        public PresentationCueKind Kind => PresentationCueKind.PlayReceivedHitPresentation;
        public bool SupportsBattle => true;
        public bool SupportsTurnStart => false;

        public UniTask ExecuteAsync(PresentationCueContext context, PresentationCue cue)
        {
            if (context.Spec.CardViewRegistry.TryGetModel(cue.SubjectId, out var receivedVictim))
            {
                context.Spec.Presentation?.PlayReceivedHit(
                    receivedVictim,
                    context.Spec.GetCardView(cue.SubjectId));
            }

            return UniTask.CompletedTask;
        }
    }

    internal sealed class PlayHeroReceivedHitPresentationCueHandler : IPresentationCueHandler
    {
        public PresentationCueKind Kind => PresentationCueKind.PlayHeroReceivedHitPresentation;
        public bool SupportsBattle => true;
        public bool SupportsTurnStart => false;

        public UniTask ExecuteAsync(PresentationCueContext context, PresentationCue cue)
        {
            var hitHero = context.ResolveHero?.Invoke(cue.SubjectHeroId);
            context.Spec.Presentation?.PlayHeroReceivedHit(
                hitHero,
                context.Spec.GetHeroTargetView(cue.SubjectHeroId));
            return UniTask.CompletedTask;
        }
    }

    internal sealed class HitShakeCueHandler : IPresentationCueHandler
    {
        public PresentationCueKind Kind => PresentationCueKind.HitShake;
        public bool SupportsBattle => true;
        public bool SupportsTurnStart => false;

        public UniTask ExecuteAsync(PresentationCueContext context, PresentationCue cue)
        {
            return PresentationCueMotionBridge.PlayHitShakeAsync(
                context.ResolveShakeView(cue),
                cue.FloatParam);
        }
    }

    internal sealed class PlayCounterOnHitPresentationCueHandler : IPresentationCueHandler
    {
        public PresentationCueKind Kind => PresentationCueKind.PlayCounterOnHitPresentation;
        public bool SupportsBattle => true;
        public bool SupportsTurnStart => false;

        public UniTask ExecuteAsync(PresentationCueContext context, PresentationCue cue)
        {
            if (cue.SourceId.IsValid
                && context.Spec.CardViewRegistry.TryGetModel(cue.SourceId, out var counterDefender))
            {
                context.Spec.Presentation?.PlayCounterOnHit(
                    counterDefender,
                    context.Spec.GetCardView(cue.SubjectId));
            }
            else if (cue.SourceHeroId.IsValid)
            {
                var heroDefender = context.ResolveHero?.Invoke(cue.SourceHeroId);
                context.Spec.Presentation?.PlayHeroCounterOnHit(
                    heroDefender,
                    context.Spec.GetCardView(cue.SubjectId));
            }

            return UniTask.CompletedTask;
        }
    }

    internal sealed class PlaySecondaryOnHitPresentationCueHandler : IPresentationCueHandler
    {
        public PresentationCueKind Kind => PresentationCueKind.PlaySecondaryOnHitPresentation;
        public bool SupportsBattle => true;
        public bool SupportsTurnStart => false;

        public UniTask ExecuteAsync(PresentationCueContext context, PresentationCue cue)
        {
            context.Spec.Presentation?.PlayMusouSecondaryOnHit(
                context.Spec.AttackerCard,
                context.Spec.GetCardView(cue.SubjectId));
            return UniTask.CompletedTask;
        }
    }

    internal sealed class CameraShakeCueHandler : IPresentationCueHandler
    {
        public PresentationCueKind Kind => PresentationCueKind.CameraShake;
        public bool SupportsBattle => true;
        public bool SupportsTurnStart => false;

        public UniTask ExecuteAsync(PresentationCueContext context, PresentationCue cue)
        {
            context.Spec.Ui?.TriggerCameraShake(cue.FloatParam);
            return UniTask.CompletedTask;
        }
    }

    internal sealed class PlayDeathPresentationCueHandler : IPresentationCueHandler
    {
        public PresentationCueKind Kind => PresentationCueKind.PlayDeathPresentation;
        public bool SupportsBattle => true;
        public bool SupportsTurnStart => false;

        public UniTask ExecuteAsync(PresentationCueContext context, PresentationCue cue)
        {
            return PresentationCueMotionBridge.PlayDeathPresentationAsync(context.Spec, cue.SubjectId);
        }
    }

    internal sealed class PlayHeroStrikePresentationCueHandler : IPresentationCueHandler
    {
        public PresentationCueKind Kind => PresentationCueKind.PlayHeroStrikePresentation;
        public bool SupportsBattle => true;
        public bool SupportsTurnStart => false;

        public UniTask ExecuteAsync(PresentationCueContext context, PresentationCue cue)
        {
            var striker = context.Spec.StrikerHero;
            context.Spec.Presentation?.PlayHeroStrike(striker, context.Spec.GetHeroTargetView(striker));
            return UniTask.CompletedTask;
        }
    }

    internal sealed class PlayHeroShieldBuffPresentationCueHandler : IPresentationCueHandler
    {
        public PresentationCueKind Kind => PresentationCueKind.PlayHeroShieldBuffPresentation;
        public bool SupportsBattle => true;
        public bool SupportsTurnStart => false;

        public UniTask ExecuteAsync(PresentationCueContext context, PresentationCue cue)
        {
            var shieldHero = context.ResolveHero?.Invoke(cue.SubjectHeroId) ?? context.Spec.StrikerHero;
            context.Spec.Presentation?.PlayHeroShieldBuff(
                shieldHero,
                context.Spec.GetHeroTargetView(shieldHero),
                cue.FloatParam);
            return UniTask.CompletedTask;
        }
    }

    internal sealed class WaitBeforeSecondaryCueHandler : IPresentationCueHandler
    {
        public PresentationCueKind Kind => PresentationCueKind.WaitBeforeSecondary;
        public bool SupportsBattle => true;
        public bool SupportsTurnStart => false;

        public UniTask ExecuteAsync(PresentationCueContext context, PresentationCue cue)
        {
            return UniTask.Delay(TimeSpan.FromSeconds(cue.Duration));
        }
    }
}
