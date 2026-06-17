using System;
using CardGame.CardBattle.Bridge;
using UnityEngine;

namespace CardGame.CardBattle.Cards
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

        public void PlayAttack(CardModel attacker, CardView attackerView)
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

        public void PlayHit(CardModel attacker, CardView targetView)
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

        public void PlayCounter(CardModel attacker, CardView attackerView)
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

        public void PlayMusouSecondaryHit(CardModel attacker, CardView targetView)
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

        public void PlayTurnHeal(CardModel healer, CardView view)
        {
            if (healer?.Behavior is HealerBehaviorAsset healerBehavior)
            {
                audio?.PlaySfx(healerBehavior.presentation.turnHealSfx);
                SpawnVfx(healerBehavior.presentation.turnHealVfxPrefab, view);
            }
        }

        public void PlayHealForTeam(CardModel[] battlefield, Func<CardModel, CardView> findView)
        {
            if (battlefield == null)
            {
                return;
            }

            for (var i = 0; i < battlefield.Length; i++)
            {
                var card = battlefield[i];
                if (card == null || !card.IsAlive || !(card.Behavior is HealerBehaviorAsset healer))
                {
                    continue;
                }

                audio?.PlaySfx(healer.presentation.turnHealSfx);
                var view = findView != null ? findView(card) : null;
                PlayTurnHeal(card, view);
                return;
            }
        }

        public void PlayDeath(CardModel card, CardView view)
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

        private void PlayAttackClip(AudioClip clip, GameObject vfxPrefab, CardView view)
        {
            audio?.PlaySfx(clip);
            SpawnVfx(vfxPrefab, view);
        }

        private void PlayHitClip(AudioClip clip, GameObject vfxPrefab, CardView view)
        {
            audio?.PlaySfx(clip);
            SpawnVfx(vfxPrefab, view);
        }

        private void PlayDeathClip(AudioClip clip, GameObject vfxPrefab, CardView view)
        {
            audio?.PlaySfx(clip);
            SpawnVfx(vfxPrefab, view);
        }

        private static void SpawnVfx(GameObject prefab, CardView view)
        {
            if (prefab == null || view == null)
            {
                return;
            }

            var instance = UnityEngine.Object.Instantiate(prefab, view.transform);
            UnityEngine.Object.Destroy(instance, DefaultVfxLifetime);
        }
    }
}
