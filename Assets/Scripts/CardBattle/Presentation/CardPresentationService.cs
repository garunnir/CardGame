using System;
using System.Collections.Generic;
using CardGame.CardBattle.Bridge;
using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Core;
using UnityEngine;

namespace CardGame.CardBattle.Presentation
{
    /// <summary>타입별 behavior presentation 기반 SFX/VFX 재생.</summary>
    public sealed class CardPresentationService
    {
        private const float DefaultVfxLifetime = 2f;

        private readonly BattleAudioAdapter audio;

        public CardPresentationService(BattleAudioAdapter audioAdapter)
        {
            audio = audioAdapter;
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
                case RangedBehaviorAsset ranged:
                    PlayAttackClip(ranged.presentation.shootSfx, ranged.presentation.projectileVfxPrefab, attackerView);
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

        public void PlayTurnHealOnTarget(CardModel healer, ICardBattleView targetView)
        {
            if (healer?.Behavior is HealerBehaviorAsset healerBehavior)
            {
                audio?.PlaySfx(healerBehavior.presentation.turnHealSfx);
                SpawnVfx(healerBehavior.presentation.turnHealVfxPrefab, targetView);
            }
        }

        public void PlayHealFromEvents(
            IReadOnlyList<TurnStartHealEvent> healEvents,
            Func<CardInstanceId, ICardBattleView> findView)
        {
            if (healEvents == null || healEvents.Count == 0)
            {
                return;
            }

            for (var i = 0; i < healEvents.Count; i++)
            {
                var healEvent = healEvents[i];
                var healer = healEvent.Healer;
                var target = healEvent.Target;
                if (healer == null || target == null)
                {
                    continue;
                }

                PlayTurnHealOnTarget(
                    healer,
                    findView != null ? findView(target.InstanceId) : null);
            }
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

        private void PlayAttackClip(AudioClip clip, GameObject vfxPrefab, ICardBattleView view)
        {
            audio?.PlaySfx(clip);
            SpawnVfx(vfxPrefab, view);
        }

        private void PlayClip(AudioClip clip, GameObject vfxPrefab, ICardBattleView view)
        {
            audio?.PlaySfx(clip);
            SpawnVfx(vfxPrefab, view);
        }

        private static void SpawnVfx(GameObject prefab, ICardBattleView view)
        {
            if (prefab == null || view == null)
            {
                return;
            }

            var anchor = view.ViewTransform;
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
                case RangedBehaviorAsset ranged:
                    sfx = ranged.presentation.hitSfx;
                    vfx = CardPresentationDefaults.ResolveOnHitVfx(ranged.presentation.hitVfxPrefab);
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
