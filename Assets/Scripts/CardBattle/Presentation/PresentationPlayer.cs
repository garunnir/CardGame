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

            for (var i = 0; i < sequence.Cues.Count; i++)
            {
                await ExecuteTurnCueAsync(sequence.Cues[i], ui, presentation, viewRegistry, heroPresenter);
            }
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
            var preparedCards = new HashSet<int>();
            var preparedHeroes = new HashSet<int>();

            for (var i = 0; i < sequence.Cues.Count; i++)
            {
                var cue = sequence.Cues[i];

                if (cue.Kind == PresentationCueKind.HpBarTween
                    && cue.SubjectId.IsValid
                    && cue.HpFrom >= 0
                    && preparedCards.Add(cue.SubjectId.Value))
                {
                    spec.GetCardView(cue.SubjectId)?.SetHpDisplay(cue.HpFrom);
                }

                if (cue.Kind == PresentationCueKind.HeroStatTween
                    && cue.SubjectHeroId.IsValid
                    && cue.HpFrom >= 0
                    && preparedHeroes.Add(cue.SubjectHeroId.Value))
                {
                    spec.HeroPresenter?.SetHeroHpDisplay(ResolveHeroForSpec(spec, cue.SubjectHeroId), cue.HpFrom);
                }
            }
        }

        private static HeroModel ResolveHeroForSpec(BattlePresentationSpec spec, HeroInstanceId id)
        {
            if (spec.PrimaryTargetHero != null && spec.PrimaryTargetHero.InstanceId == id)
            {
                return spec.PrimaryTargetHero;
            }

            if (spec.StrikerHero != null && spec.StrikerHero.InstanceId == id)
            {
                return spec.StrikerHero;
            }

            if (spec.DefenderHero != null && spec.DefenderHero.InstanceId == id)
            {
                return spec.DefenderHero;
            }

            return null;
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

                case PresentationCueKind.PlayHealOnTargetPresentation:
                    if (viewRegistry != null
                        && cue.SourceId.IsValid
                        && viewRegistry.TryGetModel(cue.SourceId, out var healer)
                        && viewRegistry.TryGetView(cue.SubjectId, out var healTargetView))
                    {
                        presentation?.PlayTurnHealOnTarget(healer, healTargetView);
                    }

                    break;

                case PresentationCueKind.PlayHeroSupportFromSlot:
                    if (viewRegistry != null
                        && cue.SubjectId.IsValid
                        && viewRegistry.TryGetModel(cue.SubjectId, out var sourceCard)
                        && heroPresenter != null)
                    {
                        var hero = heroPresenter.FindHero(cue.SubjectHeroId);
                        presentation?.PlayHeroSupportFromSlot(sourceCard, heroPresenter, hero, cue.IsMpGain);
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
                        var hero = heroPresenter.FindHero(cue.SubjectHeroId);
                        await heroPresenter.PlayStatTweenAsync(
                            hero,
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

                case PresentationCueKind.PlayShootPresentation:
                    spec.Presentation?.PlayAttack(
                        spec.AttackerCard,
                        spec.GetCardView(spec.AttackerCard.InstanceId));
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
                    spec.Presentation?.PlayHeroReceivedHit(hitHero, spec.GetHeroTargetView(hitHero));
                    break;

                case PresentationCueKind.HitShake:
                    await PlayHitShakeAsync(ResolveShakeView(spec, cue), cue.FloatParam);
                    break;

                case PresentationCueKind.HpBarTween:
                    await PlayHpBarTweenAsync(spec.GetCardView(cue.SubjectId), cue.HpFrom, cue.HpTo);
                    break;

                case PresentationCueKind.HeroStatTween:
                    if (spec.HeroPresenter != null)
                    {
                        var hero = ResolveHero(cue.SubjectHeroId);
                        await spec.HeroPresenter.PlayStatTweenAsync(
                            hero,
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

        private static IPresentationTargetView ResolveShakeView(BattlePresentationSpec spec, PresentationCue cue)
        {
            if (cue.SubjectHeroId.IsValid)
            {
                var hero = spec.HeroPresenter?.FindHero(cue.SubjectHeroId);
                return spec.GetHeroTargetView(hero);
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

                var tcs = new UniTaskCompletionSource();
                strikerView.PlayAttackDash(
                    defenderView.ViewTransform.position,
                    dashDuration,
                    null,
                    () => tcs.TrySetResult());
                await tcs.Task;
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

            var cardDashTcs = new UniTaskCompletionSource();
            attackerView.PlayAttackDash(
                targetView.ViewTransform.position,
                dashDuration,
                null,
                () => cardDashTcs.TrySetResult());
            await cardDashTcs.Task;
        }

        private static async UniTask PlayHitShakeAsync(IPresentationTargetView view, float strength)
        {
            if (view == null)
            {
                return;
            }

            var tcs = new UniTaskCompletionSource();
            view.PlayHitShake(strength, () => tcs.TrySetResult());
            await tcs.Task;
        }

        private static async UniTask PlayHpBarTweenAsync(ICardBattleView view, int fromHp, int toHp)
        {
            if (view == null || fromHp < 0 || toHp < 0)
            {
                return;
            }

            var tcs = new UniTaskCompletionSource();
            view.PlayHpChange(fromHp, toHp, () => tcs.TrySetResult());
            await tcs.Task;
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

            var tcs = new UniTaskCompletionSource();
            view.PlayDeathVisual(() => tcs.TrySetResult());
            await tcs.Task;
        }
    }
}
