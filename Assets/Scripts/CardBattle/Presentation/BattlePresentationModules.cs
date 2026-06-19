using System.Collections.Generic;
using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Core;
using UnityEngine;

namespace CardGame.CardBattle.Presentation
{
    public sealed class MeleeAttackPresentationModule : IPresentationModule
    {
        private readonly float attackDashDuration;
        private readonly float hitShakeStrength;

        public MeleeAttackPresentationModule(float attackDashDuration, float hitShakeStrength)
        {
            this.attackDashDuration = attackDashDuration;
            this.hitShakeStrength = hitShakeStrength;
        }

        public void CollectCues(PresentationContext context, IList<PresentationCue> cues)
        {
            var targetId = context.Target.InstanceId;
            cues.Add(new PresentationCue(PresentationCueKind.PlayAttackPresentation));
            cues.Add(new PresentationCue(PresentationCueKind.UiAttackBloom));
            cues.Add(new PresentationCue(PresentationCueKind.AttackDash, attackDashDuration));
            cues.Add(new PresentationCue(PresentationCueKind.PlayOnHitPresentation));
            cues.Add(new PresentationCue(
                PresentationCueKind.PlayReceivedHitPresentation,
                subjectId: targetId));
            cues.Add(new PresentationCue(
                PresentationCueKind.HitShake,
                floatParam: hitShakeStrength,
                subjectId: targetId));
            cues.Add(new PresentationCue(
                PresentationCueKind.HpBarTween,
                subjectId: targetId,
                hpFrom: context.Snapshot.GetBeforeHp(targetId),
                hpTo: context.Snapshot.GetAfterHp(targetId)));
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
            var targetId = context.Target.InstanceId;
            cues.Add(new PresentationCue(PresentationCueKind.PlayShootPresentation));
            cues.Add(new PresentationCue(PresentationCueKind.UiAttackBloom));
            cues.Add(new PresentationCue(PresentationCueKind.Wait, shootDuration > 0f ? shootDuration : 0.35f));
            cues.Add(new PresentationCue(PresentationCueKind.PlayOnHitPresentation));
            cues.Add(new PresentationCue(
                PresentationCueKind.PlayReceivedHitPresentation,
                subjectId: targetId));
            cues.Add(new PresentationCue(
                PresentationCueKind.HitShake,
                subjectId: targetId));
            cues.Add(new PresentationCue(
                PresentationCueKind.HpBarTween,
                subjectId: targetId,
                hpFrom: context.Snapshot.GetBeforeHp(targetId),
                hpTo: context.Snapshot.GetAfterHp(targetId)));
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

            var attackerId = context.Attacker.InstanceId;
            var defenderId = context.Target.InstanceId;
            cues.Add(new PresentationCue(
                PresentationCueKind.PlayCounterOnHitPresentation,
                subjectId: attackerId,
                sourceId: defenderId));
            cues.Add(new PresentationCue(
                PresentationCueKind.HitShake,
                subjectId: attackerId));
            cues.Add(new PresentationCue(
                PresentationCueKind.HpBarTween,
                subjectId: attackerId,
                hpFrom: context.Snapshot.GetBeforeHp(attackerId),
                hpTo: context.Snapshot.GetAfterHp(attackerId)));
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

            var secondaryId = context.Resolution.Secondary.Target.InstanceId;
            cues.Add(new PresentationCue(
                PresentationCueKind.WaitBeforeSecondary,
                secondaryHitDelay > 0f ? secondaryHitDelay : 0.15f));
            cues.Add(new PresentationCue(
                PresentationCueKind.PlaySecondaryOnHitPresentation,
                subjectId: secondaryId));
            cues.Add(new PresentationCue(
                PresentationCueKind.PlayReceivedHitPresentation,
                subjectId: secondaryId));
            cues.Add(new PresentationCue(
                PresentationCueKind.HitShake,
                subjectId: secondaryId));
            cues.Add(new PresentationCue(
                PresentationCueKind.HpBarTween,
                subjectId: secondaryId,
                hpFrom: context.Snapshot.GetBeforeHp(secondaryId),
                hpTo: context.Snapshot.GetAfterHp(secondaryId)));
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
