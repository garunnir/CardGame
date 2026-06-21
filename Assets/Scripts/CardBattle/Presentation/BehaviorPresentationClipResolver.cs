using CardGame.CardBattle.Cards;
using UnityEngine;

namespace CardGame.CardBattle.Presentation
{
    internal static class BehaviorPresentationClipResolver
    {
        public static void ResolveOnHit(CardBehaviorAsset behavior, out AudioClip sfx, out GameObject vfx)
        {
            CreateSource(behavior).ResolveOnHit(out sfx, out vfx);
        }

        public static void ResolveReceivedHit(CardBehaviorAsset behavior, out AudioClip sfx, out GameObject vfx)
        {
            CreateSource(behavior).ResolveReceivedHit(out sfx, out vfx);
        }

        public static void ResolveMusouSecondaryOnHit(MusouBehaviorAsset musou, out AudioClip sfx, out GameObject vfx)
        {
            if (musou == null)
            {
                sfx = null;
                vfx = null;
                return;
            }

            sfx = musou.presentation.secondaryHitSfx;
            vfx = CardPresentationDefaults.ResolveOnHitVfx(musou.presentation.secondaryHitVfxPrefab);
        }

        public static void ResolveDeath(CardBehaviorAsset behavior, out AudioClip sfx, out GameObject vfx)
        {
            CreateSource(behavior).ResolveDeath(out sfx, out vfx);
        }

        public static void ResolveAttack(CardBehaviorAsset behavior, out AudioClip sfx, out GameObject vfx)
        {
            CreateSource(behavior).ResolveAttack(out sfx, out vfx);
        }

        private static IBehaviorClipSource CreateSource(CardBehaviorAsset behavior)
        {
            return behavior switch
            {
                NormalBehaviorAsset normal => new CardBehaviorClipSource(
                    CardBehaviorClips.From(normal.presentation)),
                RangedBehaviorAsset ranged => new CardBehaviorClipSource(
                    CardBehaviorClips.FromRanged(ranged.presentation)),
                MusouBehaviorAsset musou => new CardBehaviorClipSource(
                    CardBehaviorClips.From(musou.presentation)),
                HealerBehaviorAsset healer => new CardBehaviorClipSource(
                    CardBehaviorClips.From(healer.presentation)),
                _ => EmptyClipSource.Instance,
            };
        }

        private readonly struct CardBehaviorClips
        {
            public CardBehaviorClips(
                AudioClip attackSfx,
                GameObject attackVfxPrefab,
                AudioClip hitSfx,
                GameObject hitVfxPrefab,
                AudioClip receivedHitSfx,
                GameObject receivedHitVfxPrefab,
                AudioClip deathSfx,
                GameObject deathVfxPrefab)
            {
                AttackSfx = attackSfx;
                AttackVfxPrefab = attackVfxPrefab;
                HitSfx = hitSfx;
                HitVfxPrefab = hitVfxPrefab;
                ReceivedHitSfx = receivedHitSfx;
                ReceivedHitVfxPrefab = receivedHitVfxPrefab;
                DeathSfx = deathSfx;
                DeathVfxPrefab = deathVfxPrefab;
            }

            public AudioClip AttackSfx { get; }
            public GameObject AttackVfxPrefab { get; }
            public AudioClip HitSfx { get; }
            public GameObject HitVfxPrefab { get; }
            public AudioClip ReceivedHitSfx { get; }
            public GameObject ReceivedHitVfxPrefab { get; }
            public AudioClip DeathSfx { get; }
            public GameObject DeathVfxPrefab { get; }

            public static CardBehaviorClips From(NormalBehaviorPresentation presentation)
            {
                return FromMeleeLike(
                    presentation.attackSfx,
                    presentation.attackVfxPrefab,
                    presentation.hitSfx,
                    presentation.hitVfxPrefab,
                    presentation.receivedHitSfx,
                    presentation.receivedHitVfxPrefab,
                    presentation.deathSfx,
                    presentation.deathVfxPrefab);
            }

            public static CardBehaviorClips From(MusouBehaviorPresentation presentation)
            {
                return FromMeleeLike(
                    presentation.attackSfx,
                    presentation.attackVfxPrefab,
                    presentation.hitSfx,
                    presentation.hitVfxPrefab,
                    presentation.receivedHitSfx,
                    presentation.receivedHitVfxPrefab,
                    presentation.deathSfx,
                    presentation.deathVfxPrefab);
            }

            public static CardBehaviorClips From(HealerBehaviorPresentation presentation)
            {
                return FromMeleeLike(
                    presentation.attackSfx,
                    presentation.attackVfxPrefab,
                    presentation.hitSfx,
                    presentation.hitVfxPrefab,
                    presentation.receivedHitSfx,
                    presentation.receivedHitVfxPrefab,
                    presentation.deathSfx,
                    presentation.deathVfxPrefab);
            }

            public static CardBehaviorClips FromRanged(RangedBehaviorPresentation presentation)
            {
                return new CardBehaviorClips(
                    null,
                    null,
                    null,
                    null,
                    presentation.receivedHitSfx,
                    presentation.receivedHitVfxPrefab,
                    presentation.deathSfx,
                    presentation.deathVfxPrefab);
            }

            private static CardBehaviorClips FromMeleeLike(
                AudioClip attackSfx,
                GameObject attackVfxPrefab,
                AudioClip hitSfx,
                GameObject hitVfxPrefab,
                AudioClip receivedHitSfx,
                GameObject receivedHitVfxPrefab,
                AudioClip deathSfx,
                GameObject deathVfxPrefab)
            {
                return new CardBehaviorClips(
                    attackSfx,
                    attackVfxPrefab,
                    hitSfx,
                    hitVfxPrefab,
                    receivedHitSfx,
                    receivedHitVfxPrefab,
                    deathSfx,
                    deathVfxPrefab);
            }
        }

        private interface IBehaviorClipSource
        {
            void ResolveAttack(out AudioClip sfx, out GameObject vfx);
            void ResolveOnHit(out AudioClip sfx, out GameObject vfx);
            void ResolveReceivedHit(out AudioClip sfx, out GameObject vfx);
            void ResolveDeath(out AudioClip sfx, out GameObject vfx);
        }

        private sealed class EmptyClipSource : IBehaviorClipSource
        {
            public static readonly EmptyClipSource Instance = new EmptyClipSource();

            public void ResolveAttack(out AudioClip sfx, out GameObject vfx)
            {
                sfx = null;
                vfx = null;
            }

            public void ResolveOnHit(out AudioClip sfx, out GameObject vfx)
            {
                sfx = null;
                vfx = null;
            }

            public void ResolveReceivedHit(out AudioClip sfx, out GameObject vfx)
            {
                sfx = null;
                vfx = null;
            }

            public void ResolveDeath(out AudioClip sfx, out GameObject vfx)
            {
                sfx = null;
                vfx = null;
            }
        }

        private sealed class CardBehaviorClipSource : IBehaviorClipSource
        {
            private readonly CardBehaviorClips clips;

            public CardBehaviorClipSource(CardBehaviorClips clips)
            {
                this.clips = clips;
            }

            public void ResolveAttack(out AudioClip sfx, out GameObject vfx)
            {
                sfx = clips.AttackSfx;
                vfx = clips.AttackVfxPrefab;
            }

            public void ResolveOnHit(out AudioClip sfx, out GameObject vfx)
            {
                sfx = clips.HitSfx;
                vfx = CardPresentationDefaults.ResolveOnHitVfx(clips.HitVfxPrefab);
            }

            public void ResolveReceivedHit(out AudioClip sfx, out GameObject vfx)
            {
                sfx = clips.ReceivedHitSfx;
                vfx = CardPresentationDefaults.ResolveReceivedHitVfx(clips.ReceivedHitVfxPrefab);
            }

            public void ResolveDeath(out AudioClip sfx, out GameObject vfx)
            {
                sfx = clips.DeathSfx;
                vfx = CardPresentationDefaults.ResolveDeathVfx(
                    clips.DeathVfxPrefab,
                    clips.ReceivedHitVfxPrefab);
            }
        }
    }
}
