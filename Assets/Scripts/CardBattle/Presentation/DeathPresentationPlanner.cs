using System.Collections.Generic;
using CardGame.CardBattle.Cards;

namespace CardGame.CardBattle.Presentation
{
    /// <summary>공격 연출 종료 시점에 재생할 사망 큐 — refill 전에 순서대로 삽입.</summary>
    internal static class DeathPresentationPlanner
    {
        public static void AppendDeathCues(BattlePresentationSpec spec, IList<PresentationCue> cues)
        {
            if (spec == null || cues == null || spec.Kind != PresentationKind.CardVsCard)
            {
                return;
            }

            var outcome = spec.CardOutcome;

            if (outcome.LethalTarget != null)
            {
                cues.Add(new PresentationCue(
                    PresentationCueKind.PlayDeathPresentation,
                    subjectId: outcome.LethalTarget.InstanceId));
            }
            else if (outcome.LethalAttacker != null)
            {
                cues.Add(new PresentationCue(
                    PresentationCueKind.PlayDeathPresentation,
                    subjectId: outcome.LethalAttacker.InstanceId));
            }

            if (outcome.LethalSecondary != null)
            {
                cues.Add(new PresentationCue(
                    PresentationCueKind.PlayDeathPresentation,
                    subjectId: outcome.LethalSecondary.InstanceId));
            }
        }
    }
}
