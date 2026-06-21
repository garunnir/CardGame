using System.Collections.Generic;

namespace CardGame.CardBattle.Presentation
{
    public static class TurnStartPresentationPlanner
    {
        public static void AppendCues(
            IReadOnlyList<TurnStartPresentationPlanInput> inputs,
            IList<PresentationCue> cues)
        {
            if (inputs == null || cues == null || inputs.Count == 0)
            {
                return;
            }

            var hasHeal = false;
            for (var i = 0; i < inputs.Count; i++)
            {
                if (inputs[i].Kind == TurnStartStatKind.Heal)
                {
                    hasHeal = true;
                    break;
                }
            }

            if (hasHeal)
            {
                cues.Add(new PresentationCue(PresentationCueKind.UiHealerBloom));
            }

            for (var i = 0; i < inputs.Count; i++)
            {
                var input = inputs[i];
                if (input.Kind == TurnStartStatKind.Heal)
                {
                    AppendHealFlightCue(input, cues);
                }
            }

            for (var i = 0; i < inputs.Count; i++)
            {
                var input = inputs[i];
                if (input.Kind == TurnStartStatKind.Heal)
                {
                    AppendHealImpactCues(input, cues);
                }
                else if (input.Kind == TurnStartStatKind.MpGain)
                {
                    AppendMpGainCues(input, cues);
                }
            }
        }

        private static void AppendHealFlightCue(
            TurnStartPresentationPlanInput input,
            IList<PresentationCue> cues)
        {
            if (!input.SourceId.IsValid)
            {
                return;
            }

            cues.Add(new PresentationCue(
                PresentationCueKind.FlyProjectile,
                sourceId: input.SourceId,
                subjectId: input.TargetCardId,
                subjectHeroId: input.TargetHeroId,
                projectilePresentation: input.ProjectilePresentation,
                projectileRole: ProjectileRole.TurnHeal,
                hpFrom: input.FromValue,
                hpTo: input.ToValue,
                statAmount: input.Delta));
        }

        private static void AppendHealImpactCues(
            TurnStartPresentationPlanInput input,
            IList<PresentationCue> cues)
        {
            if (input.TargetCardId.IsValid)
            {
                cues.Add(new PresentationCue(
                    PresentationCueKind.PlayProjectileImpact,
                    subjectId: input.TargetCardId,
                    sourceId: input.SourceId,
                    projectilePresentation: input.ProjectilePresentation));

                if (input.FromValue >= 0 && input.ToValue >= 0)
                {
                    PresentationStatFeedback.AppendHealFloatingText(cues, input.TargetCardId, input.Delta);

                    cues.Add(new PresentationCue(
                        PresentationCueKind.HpBarTween,
                        subjectId: input.TargetCardId,
                        hpFrom: input.FromValue,
                        hpTo: input.ToValue));
                }

                return;
            }

            if (!input.TargetHeroId.IsValid)
            {
                return;
            }

            cues.Add(new PresentationCue(
                PresentationCueKind.PlayProjectileImpact,
                subjectHeroId: input.TargetHeroId,
                sourceId: input.SourceId,
                projectilePresentation: input.ProjectilePresentation));

            if (input.FromValue >= 0 && input.ToValue >= 0)
            {
                PresentationStatFeedback.AppendHealFloatingText(cues, input.TargetHeroId, input.Delta);

                cues.Add(new PresentationCue(
                    PresentationCueKind.HeroStatTween,
                    subjectHeroId: input.TargetHeroId,
                    hpFrom: input.FromValue,
                    hpTo: input.ToValue));
            }
        }

        private static void AppendMpGainCues(
            TurnStartPresentationPlanInput input,
            IList<PresentationCue> cues)
        {
            if (!input.TargetHeroId.IsValid)
            {
                return;
            }

            cues.Add(new PresentationCue(
                PresentationCueKind.PlayHeroSupportFromSlot,
                subjectId: input.SourceId,
                subjectHeroId: input.TargetHeroId,
                isMpGain: true,
                mpFrom: input.FromValue,
                mpTo: input.ToValue));

            if (input.FromValue >= 0 && input.ToValue >= 0)
            {
                cues.Add(new PresentationCue(
                    PresentationCueKind.HeroStatTween,
                    subjectHeroId: input.TargetHeroId,
                    mpFrom: input.FromValue,
                    mpTo: input.ToValue,
                    isMpGain: true));
            }
        }
    }
}
