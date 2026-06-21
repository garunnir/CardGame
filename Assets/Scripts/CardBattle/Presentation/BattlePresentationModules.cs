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

        public void CollectCues(BattlePresentationSpec spec, IList<PresentationCue> cues)
        {
            if (spec?.PrimaryTargetCard == null)
            {
                return;
            }

            var targetId = spec.PrimaryTargetCard.InstanceId;
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
                hpFrom: spec.Snapshot.GetBeforeCardHp(targetId),
                hpTo: spec.Snapshot.GetAfterCardHp(targetId)));
        }
    }

    public sealed class CardVsHeroMeleePresentationModule : IPresentationModule
    {
        private readonly float attackDashDuration;
        private readonly float hitShakeStrength;

        public CardVsHeroMeleePresentationModule(float attackDashDuration, float hitShakeStrength)
        {
            this.attackDashDuration = attackDashDuration;
            this.hitShakeStrength = hitShakeStrength;
        }

        public void CollectCues(BattlePresentationSpec spec, IList<PresentationCue> cues)
        {
            if (spec?.PrimaryTargetHero == null)
            {
                return;
            }

            var heroId = spec.PrimaryTargetHero.InstanceId;
            cues.Add(new PresentationCue(PresentationCueKind.PlayAttackPresentation));
            cues.Add(new PresentationCue(PresentationCueKind.UiAttackBloom));
            cues.Add(new PresentationCue(PresentationCueKind.AttackDash, attackDashDuration));
            cues.Add(new PresentationCue(PresentationCueKind.PlayOnHitPresentation));
            cues.Add(new PresentationCue(
                PresentationCueKind.PlayHeroReceivedHitPresentation,
                subjectHeroId: heroId));
            cues.Add(new PresentationCue(
                PresentationCueKind.HitShake,
                floatParam: hitShakeStrength,
                subjectHeroId: heroId));
            AppendHeroStatCues(spec, heroId, cues);
        }

        internal static void AppendHeroStatCues(
            BattlePresentationSpec spec,
            HeroInstanceId heroId,
            IList<PresentationCue> cues)
        {
            if (!spec.Snapshot.TryGetHeroStats(heroId, out var stats))
            {
                return;
            }

            if (stats.BeforeHp != stats.AfterHp)
            {
                cues.Add(new PresentationCue(
                    PresentationCueKind.HeroStatTween,
                    subjectHeroId: heroId,
                    hpFrom: stats.BeforeHp,
                    hpTo: stats.AfterHp));
            }

            if (stats.BeforeShield != stats.AfterShield)
            {
                cues.Add(new PresentationCue(
                    PresentationCueKind.HeroStatTween,
                    subjectHeroId: heroId,
                    shieldFrom: stats.BeforeShield,
                    shieldTo: stats.AfterShield));
            }
        }
    }

    public sealed class RangedAttackPresentationModule : IPresentationModule
    {
        public void CollectCues(BattlePresentationSpec spec, IList<PresentationCue> cues)
        {
            if (spec?.PrimaryTargetCard == null || spec.AttackerCard == null)
            {
                return;
            }

            var attackerId = spec.AttackerCard.InstanceId;
            var targetId = spec.PrimaryTargetCard.InstanceId;
            var projectile = PresentationAssetResolve.ResolveAttack(spec.CardBehavior);

            cues.Add(new PresentationCue(PresentationCueKind.UiAttackBloom));
            cues.Add(new PresentationCue(
                PresentationCueKind.FlyProjectile,
                sourceId: attackerId,
                subjectId: targetId,
                projectilePresentation: projectile,
                projectileRole: ProjectileRole.Attack));
            cues.Add(new PresentationCue(
                PresentationCueKind.PlayProjectileImpact,
                subjectId: targetId,
                sourceId: attackerId,
                projectilePresentation: projectile));
            cues.Add(new PresentationCue(
                PresentationCueKind.PlayReceivedHitPresentation,
                subjectId: targetId));
            cues.Add(new PresentationCue(
                PresentationCueKind.HitShake,
                subjectId: targetId));
            cues.Add(new PresentationCue(
                PresentationCueKind.HpBarTween,
                subjectId: targetId,
                hpFrom: spec.Snapshot.GetBeforeCardHp(targetId),
                hpTo: spec.Snapshot.GetAfterCardHp(targetId)));
        }
    }

    public sealed class RangedCardVsHeroPresentationModule : IPresentationModule
    {
        private const float DefaultHitShake = 0.12f;

        public void CollectCues(BattlePresentationSpec spec, IList<PresentationCue> cues)
        {
            if (spec?.PrimaryTargetHero == null || spec.AttackerCard == null)
            {
                return;
            }

            var attackerId = spec.AttackerCard.InstanceId;
            var heroId = spec.PrimaryTargetHero.InstanceId;
            var projectile = PresentationAssetResolve.ResolveAttack(spec.CardBehavior);

            cues.Add(new PresentationCue(PresentationCueKind.UiAttackBloom));
            cues.Add(new PresentationCue(
                PresentationCueKind.FlyProjectile,
                sourceId: attackerId,
                subjectHeroId: heroId,
                projectilePresentation: projectile,
                projectileRole: ProjectileRole.Attack));
            cues.Add(new PresentationCue(
                PresentationCueKind.PlayProjectileImpact,
                subjectHeroId: heroId,
                sourceId: attackerId,
                projectilePresentation: projectile));
            cues.Add(new PresentationCue(
                PresentationCueKind.PlayHeroReceivedHitPresentation,
                subjectHeroId: heroId));
            cues.Add(new PresentationCue(
                PresentationCueKind.HitShake,
                floatParam: DefaultHitShake,
                subjectHeroId: heroId));
            CardVsHeroMeleePresentationModule.AppendHeroStatCues(spec, heroId, cues);
        }
    }

    public sealed class CounterPresentationModule : IPresentationModule
    {
        public void CollectCues(BattlePresentationSpec spec, IList<PresentationCue> cues)
        {
            if (spec?.AttackerCard == null || spec.CardResolution.CounterDamage <= 0)
            {
                return;
            }

            var attackerId = spec.AttackerCard.InstanceId;
            var defenderId = spec.PrimaryTargetCard.InstanceId;
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
                hpFrom: spec.Snapshot.GetBeforeCardHp(attackerId),
                hpTo: spec.Snapshot.GetAfterCardHp(attackerId)));
        }
    }

    public sealed class HeroCardCounterPresentationModule : IPresentationModule
    {
        public void CollectCues(BattlePresentationSpec spec, IList<PresentationCue> cues)
        {
            if (spec?.AttackerCard == null
                || spec.PrimaryTargetHero == null
                || spec.HeroAttackOutcome.CounterDamage <= 0)
            {
                return;
            }

            var attackerId = spec.AttackerCard.InstanceId;
            cues.Add(new PresentationCue(
                PresentationCueKind.PlayCounterOnHitPresentation,
                subjectId: attackerId,
                sourceHeroId: spec.PrimaryTargetHero.InstanceId));
            cues.Add(new PresentationCue(
                PresentationCueKind.HitShake,
                subjectId: attackerId));
            cues.Add(new PresentationCue(
                PresentationCueKind.HpBarTween,
                subjectId: attackerId,
                hpFrom: spec.Snapshot.GetBeforeCardHp(attackerId),
                hpTo: spec.Snapshot.GetAfterCardHp(attackerId)));
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

        public void CollectCues(BattlePresentationSpec spec, IList<PresentationCue> cues)
        {
            if (!spec.CardResolution.Secondary.HasTarget)
            {
                return;
            }

            var secondaryId = spec.CardResolution.Secondary.Target.InstanceId;
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
                hpFrom: spec.Snapshot.GetBeforeCardHp(secondaryId),
                hpTo: spec.Snapshot.GetAfterCardHp(secondaryId)));
            cues.Add(new PresentationCue(
                PresentationCueKind.CameraShake,
                floatParam: secondaryCameraShake));
        }
    }

    public sealed class DefaultCameraShakePresentationModule : IPresentationModule
    {
        private const float DefaultShake = 0.15f;

        public void CollectCues(BattlePresentationSpec spec, IList<PresentationCue> cues)
        {
            if (spec.CardResolution.Secondary.HasTarget)
            {
                return;
            }

            cues.Add(new PresentationCue(PresentationCueKind.CameraShake, floatParam: DefaultShake));
        }
    }

    public sealed class HeroStrikePresentationModule : IPresentationModule
    {
        private readonly float strikeDashDuration;
        private readonly float hitShakeStrength;
        private readonly float cameraShake;

        public HeroStrikePresentationModule(float strikeDashDuration, float hitShakeStrength, float cameraShake)
        {
            this.strikeDashDuration = strikeDashDuration;
            this.hitShakeStrength = hitShakeStrength;
            this.cameraShake = cameraShake;
        }

        public void CollectCues(BattlePresentationSpec spec, IList<PresentationCue> cues)
        {
            if (spec?.DefenderHero == null || spec.StrikerHero == null)
            {
                return;
            }

            var defenderId = spec.DefenderHero.InstanceId;
            cues.Add(new PresentationCue(PresentationCueKind.PlayHeroStrikePresentation));
            cues.Add(new PresentationCue(PresentationCueKind.UiAttackBloom));
            cues.Add(new PresentationCue(PresentationCueKind.AttackDash, strikeDashDuration));
            cues.Add(new PresentationCue(
                PresentationCueKind.PlayHeroReceivedHitPresentation,
                subjectHeroId: defenderId));
            cues.Add(new PresentationCue(
                PresentationCueKind.HitShake,
                floatParam: hitShakeStrength,
                subjectHeroId: defenderId));
            CardVsHeroMeleePresentationModule.AppendHeroStatCues(spec, defenderId, cues);

            if (spec.HeroStrikeResult.MpGained > 0
                && spec.Snapshot.TryGetHeroStats(spec.StrikerHero.InstanceId, out var strikerStats)
                && strikerStats.BeforeMp != strikerStats.AfterMp)
            {
                cues.Add(new PresentationCue(
                    PresentationCueKind.HeroStatTween,
                    subjectHeroId: spec.StrikerHero.InstanceId,
                    mpFrom: strikerStats.BeforeMp,
                    mpTo: strikerStats.AfterMp,
                    isMpGain: true));
            }

            cues.Add(new PresentationCue(PresentationCueKind.CameraShake, floatParam: cameraShake));
        }
    }

    public sealed class HeroShieldBuffPresentationModule : IPresentationModule
    {
        private readonly float buffBloomIntensity;

        public HeroShieldBuffPresentationModule(float buffBloomIntensity)
        {
            this.buffBloomIntensity = buffBloomIntensity;
        }

        public void CollectCues(BattlePresentationSpec spec, IList<PresentationCue> cues)
        {
            if (spec?.StrikerHero == null)
            {
                return;
            }

            var strikerId = spec.StrikerHero.InstanceId;
            cues.Add(new PresentationCue(
                PresentationCueKind.PlayHeroShieldBuffPresentation,
                floatParam: buffBloomIntensity,
                subjectHeroId: strikerId));

            if (spec.Snapshot.TryGetHeroStats(strikerId, out var stats))
            {
                if (stats.BeforeShield != stats.AfterShield)
                {
                    cues.Add(new PresentationCue(
                        PresentationCueKind.HeroStatTween,
                        subjectHeroId: strikerId,
                        shieldFrom: stats.BeforeShield,
                        shieldTo: stats.AfterShield));
                }

                if (stats.BeforeMp != stats.AfterMp)
                {
                    cues.Add(new PresentationCue(
                        PresentationCueKind.HeroStatTween,
                        subjectHeroId: strikerId,
                        mpFrom: stats.BeforeMp,
                        mpTo: stats.AfterMp));
                }
            }
        }
    }
}
