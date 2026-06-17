using System.Collections.Generic;
using CardGame.CardBattle.Cards;
using UnityEngine;

namespace CardGame.CardBattle.Presentation
{
    public sealed class MeleeAttackPresentationModule : IPresentationModule
    {
        private readonly AudioClip attackSfx;
        private readonly GameObject attackVfxPrefab;
        private readonly AudioClip hitSfx;
        private readonly GameObject hitVfxPrefab;
        private readonly float attackDashDuration;
        private readonly float hitShakeStrength;

        public MeleeAttackPresentationModule(
            AudioClip attackSfx,
            GameObject attackVfxPrefab,
            AudioClip hitSfx,
            GameObject hitVfxPrefab,
            float attackDashDuration,
            float hitShakeStrength)
        {
            this.attackSfx = attackSfx;
            this.attackVfxPrefab = attackVfxPrefab;
            this.hitSfx = hitSfx;
            this.hitVfxPrefab = hitVfxPrefab;
            this.attackDashDuration = attackDashDuration;
            this.hitShakeStrength = hitShakeStrength;
        }

        public void CollectCues(PresentationContext context, IList<PresentationCue> cues)
        {
            cues.Add(new PresentationCue(PresentationCueKind.PlayAttackPresentation));
            cues.Add(new PresentationCue(PresentationCueKind.UiAttackBloom));
            cues.Add(new PresentationCue(PresentationCueKind.AttackDash, attackDashDuration));
            cues.Add(new PresentationCue(PresentationCueKind.ApplyPrimaryDamage));
            cues.Add(new PresentationCue(PresentationCueKind.PlayHitPresentation));
            cues.Add(new PresentationCue(PresentationCueKind.HitShake, floatParam: hitShakeStrength));
            cues.Add(new PresentationCue(
                PresentationCueKind.HpBarTween,
                subject: context.Target));
        }
    }

    public sealed class RangedAttackPresentationModule : IPresentationModule
    {
        private readonly float shootDuration;

        public RangedAttackPresentationModule(float shootDuration)
        {
            this.shootDuration = shootDuration;
        }

        public void CollectCues(PresentationContext context, IList<PresentationCue> cues)
        {
            cues.Add(new PresentationCue(PresentationCueKind.PlayShootPresentation));
            cues.Add(new PresentationCue(PresentationCueKind.UiAttackBloom));
            cues.Add(new PresentationCue(PresentationCueKind.Wait, shootDuration > 0f ? shootDuration : 0.35f));
            cues.Add(new PresentationCue(PresentationCueKind.ApplyPrimaryDamage));
            cues.Add(new PresentationCue(PresentationCueKind.PlayHitPresentation));
            cues.Add(new PresentationCue(PresentationCueKind.HitShake));
            cues.Add(new PresentationCue(
                PresentationCueKind.HpBarTween,
                subject: context.Target));
        }
    }

    public sealed class CounterPresentationModule : IPresentationModule
    {
        public void CollectCues(PresentationContext context, IList<PresentationCue> cues)
        {
            if (context.Resolution.CounterDamage <= 0)
            {
                return;
            }

            cues.Add(new PresentationCue(PresentationCueKind.PlayCounterPresentation));
            cues.Add(new PresentationCue(PresentationCueKind.ApplyCounterDamage));
            cues.Add(new PresentationCue(
                PresentationCueKind.HpBarTween,
                subject: context.Attacker));
        }
    }

    public sealed class MusouSecondaryPresentationModule : IPresentationModule
    {
        private readonly float secondaryHitDelay;
        private readonly float secondaryCameraShake;

        public MusouSecondaryPresentationModule(float secondaryHitDelay, float secondaryCameraShake)
        {
            this.secondaryHitDelay = secondaryHitDelay;
            this.secondaryCameraShake = secondaryCameraShake;
        }

        public void CollectCues(PresentationContext context, IList<PresentationCue> cues)
        {
            if (!context.Resolution.Secondary.HasTarget)
            {
                return;
            }

            cues.Add(new PresentationCue(
                PresentationCueKind.WaitBeforeSecondary,
                secondaryHitDelay > 0f ? secondaryHitDelay : 0.15f));
            cues.Add(new PresentationCue(
                PresentationCueKind.PlaySecondaryHitPresentation,
                subject: context.Resolution.Secondary.Target));
            cues.Add(new PresentationCue(PresentationCueKind.ApplySecondaryDamage));
            cues.Add(new PresentationCue(
                PresentationCueKind.HpBarTween,
                subject: context.Resolution.Secondary.Target));
            cues.Add(new PresentationCue(
                PresentationCueKind.CameraShake,
                floatParam: secondaryCameraShake));
        }
    }

    public sealed class DefaultCameraShakePresentationModule : IPresentationModule
    {
        private const float DefaultShake = 0.15f;

        public void CollectCues(PresentationContext context, IList<PresentationCue> cues)
        {
            if (context.Resolution.Secondary.HasTarget)
            {
                return;
            }

            cues.Add(new PresentationCue(PresentationCueKind.CameraShake, floatParam: DefaultShake));
        }
    }
}
