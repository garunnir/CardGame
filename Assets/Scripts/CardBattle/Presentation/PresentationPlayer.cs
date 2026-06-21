using System;
using System.Collections.Generic;
using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardGame.CardBattle.Presentation
{
    public sealed class PresentationPlayer
    {
        private readonly Dictionary<int, HeroModel> heroLookup = new Dictionary<int, HeroModel>();

        public async UniTask PlayAsync(BattlePresentationSpec spec, PresentationSequence sequence)
        {
            if (spec == null || sequence == null)
            {
                return;
            }

            CacheHeroLookup(spec);
            PrepareHpDisplays(spec, sequence);

            for (var i = 0; i < sequence.Cues.Count; i++)
            {
                await ExecuteCueAsync(spec, sequence.Cues[i]);
            }
        }

        public UniTask PlayAttackAsync(PresentationContext context, PresentationSequence sequence)
        {
            if (context == null)
            {
                return UniTask.CompletedTask;
            }

            return PlayAsync(PresentationContext.ToSpec(context, 0f), sequence);
        }

        public async UniTask PlayTurnStartAsync(
            PresentationSequence sequence,
            CardGame.CardBattle.UI.UIManager ui,
            CardPresentationService presentation,
            ICardViewRegistry viewRegistry,
            HeroArenaPresenter heroPresenter = null)
        {
            if (sequence == null)
            {
                return;
            }

            PrepareTurnStartHpDisplays(sequence, viewRegistry, heroPresenter);

            for (var i = 0; i < sequence.Cues.Count; i++)
            {
                if (TryCollectParallelHealFlights(sequence.Cues, i, out var batchEnd))
                {
                    await ExecuteParallelHealFlightsAsync(
                        sequence.Cues,
                        i,
                        batchEnd,
                        presentation,
                        viewRegistry,
                        heroPresenter);
                    i = batchEnd;
                    continue;
                }

                await ExecuteTurnCueAsync(sequence.Cues[i], ui, presentation, viewRegistry, heroPresenter);
            }
        }

        private static void PrepareTurnStartHpDisplays(
            PresentationSequence sequence,
            ICardViewRegistry viewRegistry,
            HeroArenaPresenter heroPresenter)
        {
            PrepareHpDisplaysFromCues(
                sequence?.Cues,
                (id, hpFrom) =>
                {
                    if (viewRegistry != null && viewRegistry.TryGetView(id, out var cardView))
                    {
                        cardView.SetHpDisplay(hpFrom);
                    }
                },
                (heroId, hpFrom) => heroPresenter?.SetHeroHpDisplay(heroId, hpFrom));
        }

        private static bool TryCollectParallelHealFlights(
            IReadOnlyList<PresentationCue> cues,
            int startIndex,
            out int batchEndExclusive)
        {
            batchEndExclusive = startIndex;
            if (startIndex >= cues.Count)
            {
                return false;
            }

            var first = cues[startIndex];
            if (first.Kind != PresentationCueKind.FlyProjectile
                || first.ProjectileRole != ProjectileRole.TurnHeal)
            {
                return false;
            }

            var index = startIndex + 1;
            while (index < cues.Count)
            {
                var cue = cues[index];
                if (cue.Kind != PresentationCueKind.FlyProjectile
                    || cue.ProjectileRole != ProjectileRole.TurnHeal)
                {
                    break;
                }

                index++;
            }

            batchEndExclusive = index - 1;
            return index > startIndex + 1;
        }

        private static async UniTask ExecuteParallelHealFlightsAsync(
            IReadOnlyList<PresentationCue> cues,
            int startIndex,
            int endIndexInclusive,
            CardPresentationService presentation,
            ICardViewRegistry viewRegistry,
            HeroArenaPresenter heroPresenter)
        {
            if (presentation == null)
            {
                return;
            }

            var tasks = new List<UniTask>(endIndexInclusive - startIndex + 1);
            for (var i = startIndex; i <= endIndexInclusive; i++)
            {
                tasks.Add(ExecuteFlyProjectileCueAsync(
                    cues[i],
                    presentation,
                    viewRegistry,
                    heroPresenter,
                    null));
            }

            await UniTask.WhenAll(tasks);
        }

        private void CacheHeroLookup(BattlePresentationSpec spec)
        {
            heroLookup.Clear();
            RegisterHero(spec.PrimaryTargetHero);
            RegisterHero(spec.StrikerHero);
            RegisterHero(spec.DefenderHero);
        }

        private void RegisterHero(HeroModel hero)
        {
            if (hero != null && hero.InstanceId.IsValid)
            {
                heroLookup[hero.InstanceId.Value] = hero;
            }
        }

        private HeroModel ResolveHero(HeroInstanceId id)
        {
            if (!id.IsValid)
            {
                return null;
            }

            heroLookup.TryGetValue(id.Value, out var hero);
            return hero;
        }

        private static void PrepareHpDisplays(BattlePresentationSpec spec, PresentationSequence sequence)
        {
            PrepareHpDisplaysFromCues(
                sequence?.Cues,
                (id, hpFrom) => spec.GetCardView(id)?.SetHpDisplay(hpFrom),
                (heroId, hpFrom) => spec.HeroPresenter?.SetHeroHpDisplay(heroId, hpFrom));
        }

        private static void PrepareHpDisplaysFromCues(
            IReadOnlyList<PresentationCue> cues,
            Action<CardInstanceId, int> setCardHp,
            Action<HeroInstanceId, int> setHeroHp)
        {
            if (cues == null)
            {
                return;
            }

            var preparedCards = new HashSet<int>();
            var preparedHeroes = new HashSet<int>();

            for (var i = 0; i < cues.Count; i++)
            {
                var cue = cues[i];

                if (cue.Kind == PresentationCueKind.HpBarTween
                    && cue.SubjectId.IsValid
                    && cue.HpFrom >= 0
                    && preparedCards.Add(cue.SubjectId.Value))
                {
                    setCardHp?.Invoke(cue.SubjectId, cue.HpFrom);
                }

                if (cue.Kind == PresentationCueKind.HeroStatTween
                    && cue.SubjectHeroId.IsValid
                    && cue.HpFrom >= 0
                    && preparedHeroes.Add(cue.SubjectHeroId.Value))
                {
                    setHeroHp?.Invoke(cue.SubjectHeroId, cue.HpFrom);
                }
            }
        }

        private async UniTask ExecuteTurnCueAsync(
            PresentationCue cue,
            CardGame.CardBattle.UI.UIManager ui,
            CardPresentationService presentation,
            ICardViewRegistry viewRegistry,
            HeroArenaPresenter heroPresenter)
        {
            switch (cue.Kind)
            {
                case PresentationCueKind.UiHealerBloom:
                    ui?.PulseHealerBloom();
                    break;

                case PresentationCueKind.FlyProjectile:
                    await ExecuteFlyProjectileCueAsync(
                        cue,
                        presentation,
                        viewRegistry,
                        heroPresenter,
                        null);
                    break;

                case PresentationCueKind.PlayProjectileImpact:
                    ExecuteProjectileImpactCue(cue, presentation, viewRegistry, heroPresenter);
                    break;

                case PresentationCueKind.PlayStatFloatingText:
                    await ExecuteStatFloatingTextCueAsync(cue, presentation, viewRegistry, heroPresenter);
                    break;

                case PresentationCueKind.PlayHeroSupportFromSlot:
                    if (cue.IsMpGain)
                    {
                        presentation?.PlayMpGainStub();
                    }

                    break;

                case PresentationCueKind.HpBarTween:
                    if (viewRegistry != null && viewRegistry.TryGetView(cue.SubjectId, out var healTarget))
                    {
                        healTarget.SetHpDisplay(cue.HpFrom);
                        await PlayHpBarTweenAsync(healTarget, cue.HpFrom, cue.HpTo);
                    }

                    break;

                case PresentationCueKind.HeroStatTween:
                    if (heroPresenter != null)
                    {
                        await heroPresenter.PlayStatTweenAsync(
                            cue.SubjectHeroId,
                            cue.HpFrom,
                            cue.HpTo,
                            cue.ShieldFrom,
                            cue.ShieldTo,
                            cue.MpFrom,
                            cue.MpTo);
                    }

                    break;

                case PresentationCueKind.Wait:
                    await UniTask.Delay(TimeSpan.FromSeconds(cue.Duration));
                    break;
            }
        }

        private async UniTask ExecuteCueAsync(BattlePresentationSpec spec, PresentationCue cue)
        {
            switch (cue.Kind)
            {
                case PresentationCueKind.Wait:
                case PresentationCueKind.WaitBeforeSecondary:
                    await UniTask.Delay(TimeSpan.FromSeconds(cue.Duration));
                    break;

                case PresentationCueKind.UiAttackBloom:
                    spec.Ui?.PulseAttackBloom();
                    break;

                case PresentationCueKind.PlayAttackPresentation:
                    spec.Presentation?.PlayAttack(
                        spec.AttackerCard,
                        spec.GetCardView(spec.AttackerCard.InstanceId));
                    break;

                case PresentationCueKind.FlyProjectile:
                    await ExecuteFlyProjectileCueAsync(
                        cue,
                        spec.Presentation,
                        spec.CardViewRegistry,
                        spec.HeroPresenter,
                        spec);
                    break;

                case PresentationCueKind.PlayProjectileImpact:
                    ExecuteProjectileImpactCue(
                        cue,
                        spec.Presentation,
                        spec.CardViewRegistry,
                        spec.HeroPresenter);
                    break;

                case PresentationCueKind.AttackDash:
                    await PlayAttackDashAsync(spec, cue.Duration);
                    break;

                case PresentationCueKind.PlayOnHitPresentation:
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

                    break;

                case PresentationCueKind.PlayReceivedHitPresentation:
                    if (spec.CardViewRegistry.TryGetModel(cue.SubjectId, out var receivedVictim))
                    {
                        spec.Presentation?.PlayReceivedHit(
                            receivedVictim,
                            spec.GetCardView(cue.SubjectId));
                    }

                    break;

                case PresentationCueKind.PlayHeroReceivedHitPresentation:
                    var hitHero = ResolveHero(cue.SubjectHeroId);
                    spec.Presentation?.PlayHeroReceivedHit(hitHero, spec.GetHeroTargetView(cue.SubjectHeroId));
                    break;

                case PresentationCueKind.HitShake:
                    await PlayHitShakeAsync(ResolveShakeView(spec, cue), cue.FloatParam);
                    break;

                case PresentationCueKind.PlayStatFloatingText:
                    await ExecuteStatFloatingTextCueAsync(
                        cue,
                        spec.Presentation,
                        spec.CardViewRegistry,
                        spec.HeroPresenter);
                    break;

                case PresentationCueKind.HpBarTween:
                    await PlayHpBarTweenAsync(spec.GetCardView(cue.SubjectId), cue.HpFrom, cue.HpTo);
                    break;

                case PresentationCueKind.HeroStatTween:
                    if (spec.HeroPresenter != null)
                    {
                        await spec.HeroPresenter.PlayStatTweenAsync(
                            cue.SubjectHeroId,
                            cue.HpFrom,
                            cue.HpTo,
                            cue.ShieldFrom,
                            cue.ShieldTo,
                            cue.MpFrom,
                            cue.MpTo);
                    }

                    break;

                case PresentationCueKind.PlayCounterOnHitPresentation:
                    if (cue.SourceId.IsValid && spec.CardViewRegistry.TryGetModel(cue.SourceId, out var counterDefender))
                    {
                        spec.Presentation?.PlayCounterOnHit(
                            counterDefender,
                            spec.GetCardView(cue.SubjectId));
                    }
                    else if (cue.SourceHeroId.IsValid)
                    {
                        var heroDefender = ResolveHero(cue.SourceHeroId);
                        spec.Presentation?.PlayHeroCounterOnHit(
                            heroDefender,
                            spec.GetCardView(cue.SubjectId));
                    }

                    break;

                case PresentationCueKind.PlaySecondaryOnHitPresentation:
                    spec.Presentation?.PlayMusouSecondaryOnHit(
                        spec.AttackerCard,
                        spec.GetCardView(cue.SubjectId));
                    break;

                case PresentationCueKind.CameraShake:
                    spec.Ui?.TriggerCameraShake(cue.FloatParam);
                    break;

                case PresentationCueKind.PlayDeathPresentation:
                    await PlayDeathPresentationAsync(spec, cue.SubjectId);
                    break;

                case PresentationCueKind.PlayHeroStrikePresentation:
                    var striker = spec.StrikerHero;
                    spec.Presentation?.PlayHeroStrike(striker, spec.GetHeroTargetView(striker));
                    break;

                case PresentationCueKind.PlayHeroShieldBuffPresentation:
                    var shieldHero = ResolveHero(cue.SubjectHeroId) ?? spec.StrikerHero;
                    spec.Presentation?.PlayHeroShieldBuff(
                        shieldHero,
                        spec.GetHeroTargetView(shieldHero),
                        cue.FloatParam);
                    break;

                default:
                    break;
            }
        }

        private static async UniTask ExecuteFlyProjectileCueAsync(
            PresentationCue cue,
            CardPresentationService presentation,
            ICardViewRegistry viewRegistry,
            HeroArenaPresenter heroPresenter,
            BattlePresentationSpec spec)
        {
            if (presentation == null || cue.ProjectilePresentation == null || !cue.SourceId.IsValid)
            {
                return;
            }

            var fromView = ResolveCardTargetView(viewRegistry, spec, cue.SourceId);
            var toView = ResolveCueTargetView(cue, viewRegistry, heroPresenter, spec);
            if (fromView == null || toView == null)
            {
                return;
            }

            await presentation.FlyProjectileAsync(cue.ProjectilePresentation, fromView, toView);
        }

        private static void ExecuteProjectileImpactCue(
            PresentationCue cue,
            CardPresentationService presentation,
            ICardViewRegistry viewRegistry,
            HeroArenaPresenter heroPresenter,
            BattlePresentationSpec spec = null)
        {
            if (presentation == null)
            {
                return;
            }

            var targetView = ResolveCueTargetView(cue, viewRegistry, heroPresenter, spec);
            presentation.PlayProjectileImpact(cue.ProjectilePresentation, targetView);
        }

        private static async UniTask ExecuteStatFloatingTextCueAsync(
            PresentationCue cue,
            CardPresentationService presentation,
            ICardViewRegistry viewRegistry,
            HeroArenaPresenter heroPresenter)
        {
            if (presentation == null || cue.StatAmount <= 0)
            {
                return;
            }

            var targetView = ResolveCueTargetView(cue, viewRegistry, heroPresenter, null);
            if (targetView == null)
            {
                return;
            }

            await presentation.ShowStatFloatingTextAsync(targetView, cue.StatFeedbackKind, cue.StatAmount);
        }

        private static IPresentationTargetView ResolveCueTargetView(
            PresentationCue cue,
            ICardViewRegistry viewRegistry,
            HeroArenaPresenter heroPresenter,
            BattlePresentationSpec spec)
        {
            if (cue.SubjectHeroId.IsValid)
            {
                return heroPresenter != null
                    ? heroPresenter.GetPresentationView(cue.SubjectHeroId)
                    : spec?.GetHeroTargetView(cue.SubjectHeroId);
            }

            if (cue.SubjectId.IsValid)
            {
                return ResolveCardTargetView(viewRegistry, spec, cue.SubjectId);
            }

            return null;
        }

        private static IPresentationTargetView ResolveCardTargetView(
            ICardViewRegistry viewRegistry,
            BattlePresentationSpec spec,
            CardInstanceId id)
        {
            if (spec != null)
            {
                return spec.GetCardTargetView(id);
            }

            if (viewRegistry != null && viewRegistry.TryGetView(id, out var view))
            {
                return new CardPresentationTargetAdapter(view);
            }

            return null;
        }

        private static IPresentationTargetView ResolveShakeView(BattlePresentationSpec spec, PresentationCue cue)
        {
            if (cue.SubjectHeroId.IsValid)
            {
                return spec.GetHeroTargetView(cue.SubjectHeroId);
            }

            if (cue.SubjectId.IsValid)
            {
                return spec.GetCardTargetView(cue.SubjectId);
            }

            if (spec.PrimaryTargetHero != null)
            {
                return spec.GetHeroTargetView(spec.PrimaryTargetHero);
            }

            return spec.GetCardTargetView(spec.PrimaryTargetCard?.InstanceId ?? default);
        }

        private static async UniTask PlayAttackDashAsync(BattlePresentationSpec spec, float dashDuration)
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

        private static UniTask PlayHitShakeAsync(IPresentationTargetView view, float strength)
        {
            if (view == null)
            {
                return UniTask.CompletedTask;
            }

            return PresentationAsyncBridge.FromCallback(onComplete =>
                view.PlayHitShake(strength, onComplete));
        }

        private static UniTask PlayHpBarTweenAsync(ICardBattleView view, int fromHp, int toHp)
        {
            if (view == null || fromHp < 0 || toHp < 0)
            {
                return UniTask.CompletedTask;
            }

            return PresentationAsyncBridge.FromCallback(onComplete =>
                view.PlayHpChange(fromHp, toHp, onComplete));
        }

        private static async UniTask PlayDeathPresentationAsync(BattlePresentationSpec spec, CardInstanceId subjectId)
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
