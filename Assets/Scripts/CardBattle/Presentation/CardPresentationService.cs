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

            PlayClipOnTarget(presentation.impactSfx, presentation.impactVfxPrefab, targetView);
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

            switch (attacker.Behavior)
            {
                case NormalBehaviorAsset normal:
                    PlayAttackClip(normal.presentation.attackSfx, normal.presentation.attackVfxPrefab, attackerView);
                    break;
                case RangedBehaviorAsset:
                    break;
                case MusouBehaviorAsset musou:
                    PlayAttackClip(musou.presentation.attackSfx, musou.presentation.attackVfxPrefab, attackerView);
                    break;
                case HealerBehaviorAsset healer:
                    PlayAttackClip(healer.presentation.attackSfx, healer.presentation.attackVfxPrefab, attackerView);
                    break;
            }
        }

        /// <summary>공격자 SO 명중 → 타겟 앵커.</summary>
        public void PlayOnHit(CardModel attacker, ICardBattleView targetView)
        {
            if (attacker == null)
            {
                return;
            }

            BehaviorPresentationClips.ResolveOnHit(attacker.Behavior, out var sfx, out var vfx);
            PlayClip(sfx, vfx, targetView);
        }

        /// <summary>피해자 SO 피격 → 피해자 앵커.</summary>
        public void PlayReceivedHit(CardModel victim, ICardBattleView victimView)
        {
            if (victim == null)
            {
                return;
            }

            BehaviorPresentationClips.ResolveReceivedHit(victim.Behavior, out var sfx, out var vfx);
            PlayClip(sfx, vfx, victimView);
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

            BehaviorPresentationClips.ResolveMusouSecondaryOnHit(musou, out var sfx, out var vfx);
            PlayClip(sfx, vfx, targetView);
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

            BehaviorPresentationClips.ResolveDeath(card.Behavior, out var sfx, out var vfx);
            PlayClip(sfx, vfx, view);
        }

        public void PlayOnHitToHero(CardModel attacker, IPresentationTargetView heroView)
        {
            if (attacker == null)
            {
                return;
            }

            BehaviorPresentationClips.ResolveOnHit(attacker.Behavior, out var sfx, out var vfx);
            PlayClipOnTarget(sfx, vfx, heroView);
        }

        public void PlayHeroReceivedHit(HeroModel hero, IPresentationTargetView heroView)
        {
            if (hero == null)
            {
                return;
            }

            AudioClip sfx = null;
            GameObject vfx = null;
            if (hero.NormalAttackBehavior != null)
            {
                sfx = hero.NormalAttackBehavior.presentation.receivedHitSfx;
                vfx = hero.NormalAttackBehavior.presentation.receivedHitVfxPrefab;
            }

            PlayClipOnTarget(sfx, vfx, heroView);
        }

        public void PlayHeroCounterOnHit(HeroModel defender, ICardBattleView attackerView)
        {
            PlayHeroReceivedHit(defender, attackerView != null ? new CardPresentationTargetAdapter(attackerView) : null);
        }

        public void PlayHeroStrike(HeroModel striker, IPresentationTargetView strikerView)
        {
            if (striker?.NormalAttackBehavior == null)
            {
                return;
            }

            var presentation = striker.NormalAttackBehavior.presentation;
            audio?.PlaySfx(presentation.strikeSfx);
            SpawnVfxOnTarget(presentation.strikeVfxPrefab, strikerView);
        }

        public void PlayHeroShieldBuff(HeroModel striker, IPresentationTargetView strikerView, float bloomIntensity)
        {
            if (striker?.ShieldBehavior == null)
            {
                return;
            }

            var presentation = striker.ShieldBehavior.presentation;
            audio?.PlaySfx(presentation.shieldBuffSfx);
            SpawnVfxOnTarget(presentation.shieldBuffVfxPrefab, strikerView);
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

        private void PlayClipOnTarget(AudioClip clip, GameObject vfxPrefab, IPresentationTargetView view)
        {
            audio?.PlaySfx(clip);
            SpawnVfxAtTarget(vfxPrefab, view?.ViewTransform);
        }

        private void PlayAttackClip(AudioClip clip, GameObject vfxPrefab, ICardBattleView view)
        {
            audio?.PlaySfx(clip);
            SpawnVfxAtTarget(vfxPrefab, view?.ViewTransform);
        }

        private void PlayClip(AudioClip clip, GameObject vfxPrefab, ICardBattleView view)
        {
            audio?.PlaySfx(clip);
            SpawnVfxAtTarget(vfxPrefab, view?.ViewTransform);
        }

        private static void SpawnVfxOnTarget(GameObject prefab, IPresentationTargetView view)
        {
            SpawnVfxAtTarget(prefab, view?.ViewTransform);
        }

        private static void SpawnVfxAtTarget(GameObject prefab, Transform anchor)
        {
            if (prefab == null || anchor == null)
            {
                return;
            }

            var instance = UnityEngine.Object.Instantiate(prefab, anchor.position, anchor.rotation);
            UnityEngine.Object.Destroy(instance, DefaultVfxLifetime);
        }
    }

    internal static class BehaviorPresentationClips
    {
        public static void ResolveOnHit(CardBehaviorAsset behavior, out AudioClip sfx, out GameObject vfx)
        {
            sfx = null;
            vfx = null;
            if (behavior == null)
            {
                return;
            }

            switch (behavior)
            {
                case NormalBehaviorAsset normal:
                    sfx = normal.presentation.hitSfx;
                    vfx = CardPresentationDefaults.ResolveOnHitVfx(normal.presentation.hitVfxPrefab);
                    break;
                case RangedBehaviorAsset:
                    break;
                case MusouBehaviorAsset musou:
                    sfx = musou.presentation.hitSfx;
                    vfx = CardPresentationDefaults.ResolveOnHitVfx(musou.presentation.hitVfxPrefab);
                    break;
                case HealerBehaviorAsset healer:
                    sfx = healer.presentation.hitSfx;
                    vfx = CardPresentationDefaults.ResolveOnHitVfx(healer.presentation.hitVfxPrefab);
                    break;
            }
        }

        public static void ResolveReceivedHit(CardBehaviorAsset behavior, out AudioClip sfx, out GameObject vfx)
        {
            sfx = null;
            vfx = null;
            if (behavior == null)
            {
                return;
            }

            switch (behavior)
            {
                case NormalBehaviorAsset normal:
                    sfx = normal.presentation.receivedHitSfx;
                    vfx = CardPresentationDefaults.ResolveReceivedHitVfx(normal.presentation.receivedHitVfxPrefab);
                    break;
                case RangedBehaviorAsset ranged:
                    sfx = ranged.presentation.receivedHitSfx;
                    vfx = CardPresentationDefaults.ResolveReceivedHitVfx(ranged.presentation.receivedHitVfxPrefab);
                    break;
                case MusouBehaviorAsset musou:
                    sfx = musou.presentation.receivedHitSfx;
                    vfx = CardPresentationDefaults.ResolveReceivedHitVfx(musou.presentation.receivedHitVfxPrefab);
                    break;
                case HealerBehaviorAsset healer:
                    sfx = healer.presentation.receivedHitSfx;
                    vfx = CardPresentationDefaults.ResolveReceivedHitVfx(healer.presentation.receivedHitVfxPrefab);
                    break;
            }
        }

        public static void ResolveMusouSecondaryOnHit(MusouBehaviorAsset musou, out AudioClip sfx, out GameObject vfx)
        {
            sfx = null;
            vfx = null;
            if (musou == null)
            {
                return;
            }

            sfx = musou.presentation.secondaryHitSfx;
            vfx = CardPresentationDefaults.ResolveOnHitVfx(musou.presentation.secondaryHitVfxPrefab);
        }

        public static void ResolveDeath(CardBehaviorAsset behavior, out AudioClip sfx, out GameObject vfx)
        {
            sfx = null;
            vfx = null;
            if (behavior == null)
            {
                return;
            }

            switch (behavior)
            {
                case NormalBehaviorAsset normal:
                    sfx = normal.presentation.deathSfx;
                    vfx = CardPresentationDefaults.ResolveDeathVfx(
                        normal.presentation.deathVfxPrefab,
                        normal.presentation.receivedHitVfxPrefab);
                    break;
                case RangedBehaviorAsset ranged:
                    sfx = ranged.presentation.deathSfx;
                    vfx = CardPresentationDefaults.ResolveDeathVfx(
                        ranged.presentation.deathVfxPrefab,
                        ranged.presentation.receivedHitVfxPrefab);
                    break;
                case MusouBehaviorAsset musou:
                    sfx = musou.presentation.deathSfx;
                    vfx = CardPresentationDefaults.ResolveDeathVfx(
                        musou.presentation.deathVfxPrefab,
                        musou.presentation.receivedHitVfxPrefab);
                    break;
                case HealerBehaviorAsset healer:
                    sfx = healer.presentation.deathSfx;
                    vfx = CardPresentationDefaults.ResolveDeathVfx(
                        healer.presentation.deathVfxPrefab,
                        healer.presentation.receivedHitVfxPrefab);
                    break;
            }
        }
    }
}
