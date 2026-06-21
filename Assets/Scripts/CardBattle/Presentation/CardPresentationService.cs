using System;
using System.Collections.Generic;
using CardGame.CardBattle.Bridge;
using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardGame.CardBattle.Presentation
{
    /// <summary>타입별 behavior presentation 기반 SFX/VFX 재생.</summary>
    public sealed class CardPresentationService
    {
        private const float DefaultVfxLifetime = 2f;

        private readonly BattleAudioAdapter audio;
        private readonly StatFloatingTextPresentationAsset statFloatingText;

        public CardPresentationService(
            BattleAudioAdapter audioAdapter,
            StatFloatingTextPresentationAsset statFloatingTextPresentation = null)
        {
            audio = audioAdapter;
            statFloatingText = statFloatingTextPresentation ?? StatFloatingTextPresentationAsset.LoadDefault();
        }

        public UniTask FlyProjectileAsync(
            ProjectilePresentationAsset presentation,
            IPresentationTargetView fromView,
            IPresentationTargetView toView)
        {
            if (presentation == null || fromView?.ViewTransform == null || toView?.ViewTransform == null)
            {
                return UniTask.CompletedTask;
            }

            audio?.PlaySfx(presentation.launchSfx);
            return PresentationProjectileFlight.FlyAsync(
                fromView.ViewTransform,
                toView.ViewTransform,
                presentation.projectileVfxPrefab,
                presentation.flightDuration,
                presentation.pathKind,
                presentation.arcHeight);
        }

        public void PlayProjectileImpact(
            ProjectilePresentationAsset presentation,
            IPresentationTargetView targetView)
        {
            if (presentation == null)
            {
                return;
            }

            PlayClipAt(presentation.impactSfx, presentation.impactVfxPrefab, targetView?.ViewTransform);
        }

        public UniTask ShowStatFloatingTextAsync(
            IPresentationTargetView targetView,
            StatFeedbackKind kind,
            int amount)
        {
            return BattleStatFloatingTextPresenter.ShowAsync(statFloatingText, targetView, kind, amount);
        }

        public void PlayTurnStartEffectsImmediate(
            IReadOnlyList<TurnStartPresentationPlanInput> inputs,
            Func<CardInstanceId, ICardBattleView> findCardView,
            HeroArenaPresenter heroPresenter)
        {
            if (inputs == null || inputs.Count == 0)
            {
                return;
            }

            for (var i = 0; i < inputs.Count; i++)
            {
                var input = inputs[i];
                if (input.Kind != TurnStartStatKind.Heal)
                {
                    if (input.Kind == TurnStartStatKind.MpGain)
                    {
                        PlayMpGainStub();
                    }

                    continue;
                }

                var targetView = ResolvePlanTargetView(input, findCardView, heroPresenter);
                if (targetView == null)
                {
                    continue;
                }

                PlayProjectileImpact(input.ProjectilePresentation, targetView);
                ShowStatFloatingTextAsync(targetView, StatFeedbackKind.Heal, input.Delta).Forget();
            }
        }

        public void PlayAttack(CardModel attacker, ICardBattleView attackerView)
        {
            if (attacker == null)
            {
                return;
            }

            BehaviorPresentationClipResolver.ResolveAttack(attacker.Behavior, out var sfx, out var vfx);
            PlayClipAt(sfx, vfx, attackerView?.ViewTransform);
        }

        /// <summary>공격자 SO 명중 → 타겟 앵커.</summary>
        public void PlayOnHit(CardModel attacker, ICardBattleView targetView)
        {
            if (attacker == null)
            {
                return;
            }

            BehaviorPresentationClipResolver.ResolveOnHit(attacker.Behavior, out var sfx, out var vfx);
            PlayClipAt(sfx, vfx, targetView?.ViewTransform);
        }

        /// <summary>피해자 SO 피격 → 피해자 앵커.</summary>
        public void PlayReceivedHit(CardModel victim, ICardBattleView victimView)
        {
            if (victim == null)
            {
                return;
            }

            BehaviorPresentationClipResolver.ResolveReceivedHit(victim.Behavior, out var sfx, out var vfx);
            PlayClipAt(sfx, vfx, victimView?.ViewTransform);
        }

        /// <summary>방어자 SO 명중(반격) → 공격자 앵커.</summary>
        public void PlayCounterOnHit(CardModel defender, ICardBattleView attackerView)
        {
            PlayOnHit(defender, attackerView);
        }

        /// <summary>공격자 SO 2타 명중 → 2타 타겟 앵커.</summary>
        public void PlayMusouSecondaryOnHit(CardModel attacker, ICardBattleView targetView)
        {
            if (attacker?.Behavior is not MusouBehaviorAsset musou)
            {
                return;
            }

            BehaviorPresentationClipResolver.ResolveMusouSecondaryOnHit(musou, out var sfx, out var vfx);
            PlayClipAt(sfx, vfx, targetView?.ViewTransform);
        }

        public float GetMusouSecondaryCameraShake(CardModel attacker)
        {
            if (attacker?.Behavior is MusouBehaviorAsset musou)
            {
                return musou.presentation.secondaryCameraShake;
            }

            return 0f;
        }

        public void PlayDeath(CardModel card, ICardBattleView view)
        {
            if (card == null)
            {
                return;
            }

            BehaviorPresentationClipResolver.ResolveDeath(card.Behavior, out var sfx, out var vfx);
            PlayClipAt(sfx, vfx, view?.ViewTransform);
        }

        public void PlayOnHitToHero(CardModel attacker, IPresentationTargetView heroView)
        {
            if (attacker == null)
            {
                return;
            }

            BehaviorPresentationClipResolver.ResolveOnHit(attacker.Behavior, out var sfx, out var vfx);
            PlayClipAt(sfx, vfx, heroView?.ViewTransform);
        }

        public void PlayHeroReceivedHit(HeroModel hero, IPresentationTargetView heroView)
        {
            if (hero == null)
            {
                return;
            }

            HeroPresentationClipResolver.ResolveReceivedHit(hero, out var sfx, out var vfx);
            PlayClipAt(sfx, vfx, heroView?.ViewTransform);
        }

        public void PlayHeroCounterOnHit(HeroModel defender, ICardBattleView attackerView)
        {
            PlayHeroReceivedHit(defender, attackerView != null ? new CardPresentationTargetAdapter(attackerView) : null);
        }

        public void PlayHeroStrike(HeroModel striker, IPresentationTargetView strikerView)
        {
            HeroPresentationClipResolver.ResolveStrike(striker, out var sfx, out var vfx);
            PlayClipAt(sfx, vfx, strikerView?.ViewTransform);
        }

        public void PlayHeroShieldBuff(HeroModel striker, IPresentationTargetView strikerView, float bloomIntensity)
        {
            HeroPresentationClipResolver.ResolveShieldBuff(striker, out var sfx, out var vfx);
            PlayClipAt(sfx, vfx, strikerView?.ViewTransform);
        }

        public void PlayMpGainStub()
        {
            audio?.PlaySfx(null);
        }

        private static IPresentationTargetView ResolvePlanTargetView(
            TurnStartPresentationPlanInput input,
            Func<CardInstanceId, ICardBattleView> findCardView,
            HeroArenaPresenter heroPresenter)
        {
            if (input.TargetCardId.IsValid && findCardView != null)
            {
                var cardView = findCardView(input.TargetCardId);
                return cardView != null ? new CardPresentationTargetAdapter(cardView) : null;
            }

            if (input.TargetHeroId.IsValid && heroPresenter != null)
            {
                return heroPresenter.GetPresentationView(input.TargetHeroId);
            }

            return null;
        }

        private void PlayClipAt(AudioClip clip, GameObject vfxPrefab, Transform anchor)
        {
            audio?.PlaySfx(clip);
            SpawnVfxAtTarget(vfxPrefab, anchor);
        }

        private static void SpawnVfxAtTarget(GameObject prefab, Transform anchor)
        {
            if (prefab == null || anchor == null)
            {
                return;
            }

            var instance = PresentationVfxSpawn.InstantiateAt(prefab, anchor);
            if (instance == null)
            {
                return;
            }

            UnityEngine.Object.Destroy(instance, DefaultVfxLifetime);
        }
    }
}
