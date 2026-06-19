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

        public void PlayHit(CardModel attacker, ICardBattleView targetView)
        {
            if (attacker == null)
            {
                return;
            }

            switch (attacker.Behavior)
            {
                case NormalBehaviorAsset normal:
                    PlayHitClip(normal.presentation.hitSfx, normal.presentation.hitVfxPrefab, targetView);
                    break;
                case RangedBehaviorAsset ranged:
                    PlayHitClip(ranged.presentation.hitSfx, ranged.presentation.hitVfxPrefab, targetView);
                    break;
                case MusouBehaviorAsset musou:
                    PlayHitClip(musou.presentation.hitSfx, musou.presentation.hitVfxPrefab, targetView);
                    break;
                case HealerBehaviorAsset healer:
                    PlayHitClip(healer.presentation.hitSfx, healer.presentation.hitVfxPrefab, targetView);
                    break;
            }
        }

        public void PlayCounter(CardModel attacker, ICardBattleView attackerView)
        {
            if (attacker == null)
            {
                return;
            }

            switch (attacker.Behavior)
            {
                case NormalBehaviorAsset normal:
                    audio?.PlaySfx(normal.presentation.counterSfx);
                    break;
                case MusouBehaviorAsset musou:
                    audio?.PlaySfx(musou.presentation.counterSfx);
                    break;
                case HealerBehaviorAsset healer:
                    audio?.PlaySfx(healer.presentation.counterSfx);
                    break;
            }
        }

        public void PlayMusouSecondaryHit(CardModel attacker, ICardBattleView targetView)
        {
            if (attacker?.Behavior is MusouBehaviorAsset musou)
            {
                PlayHitClip(
                    musou.presentation.secondaryHitSfx,
                    musou.presentation.secondaryHitVfxPrefab,
                    targetView);
            }
        }

        public float GetMusouSecondaryCameraShake(CardModel attacker)
        {
            if (attacker?.Behavior is MusouBehaviorAsset musou)
            {
                return musou.presentation.secondaryCameraShake;
            }

            return 0f;
        }

        public void PlayTurnHeal(CardModel healer, ICardBattleView view)
        {
            if (healer?.Behavior is HealerBehaviorAsset healerBehavior)
            {
                audio?.PlaySfx(healerBehavior.presentation.turnHealSfx);
                SpawnVfx(healerBehavior.presentation.turnHealVfxPrefab, view);
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

            var seenHealers = new HashSet<CardModel>();
            for (var i = 0; i < healEvents.Count; i++)
            {
                var healer = healEvents[i].Healer;
                if (healer == null || !seenHealers.Add(healer))
                {
                    continue;
                }

                PlayTurnHeal(healer, findView != null ? findView(healer.InstanceId) : null);
            }
        }

        public void PlayDeath(CardModel card, ICardBattleView view)
        {
            if (card == null)
            {
                return;
            }

            switch (card.Behavior)
            {
                case NormalBehaviorAsset normal:
                    PlayDeathClip(normal.presentation.deathSfx, normal.presentation.hitVfxPrefab, view);
                    break;
                case RangedBehaviorAsset ranged:
                    PlayDeathClip(ranged.presentation.deathSfx, ranged.presentation.hitVfxPrefab, view);
                    break;
                case MusouBehaviorAsset musou:
                    PlayDeathClip(musou.presentation.deathSfx, musou.presentation.hitVfxPrefab, view);
                    break;
                case HealerBehaviorAsset healer:
                    PlayDeathClip(healer.presentation.deathSfx, healer.presentation.hitVfxPrefab, view);
                    break;
            }
        }

        private void PlayAttackClip(AudioClip clip, GameObject vfxPrefab, ICardBattleView view)
        {
            audio?.PlaySfx(clip);
            SpawnVfx(vfxPrefab, view);
        }

        private void PlayHitClip(AudioClip clip, GameObject vfxPrefab, ICardBattleView view)
        {
            audio?.PlaySfx(clip);
            SpawnVfx(vfxPrefab, view);
        }

        private void PlayDeathClip(AudioClip clip, GameObject vfxPrefab, ICardBattleView view)
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
}
