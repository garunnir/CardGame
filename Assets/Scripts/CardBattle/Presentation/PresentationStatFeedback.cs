using System.Collections.Generic;
using CardGame.CardBattle.Core;

namespace CardGame.CardBattle.Presentation
{
    public static class PresentationStatFeedback
    {
        public static void AppendDamageFloatingText(
            IList<PresentationCue> cues,
            CardInstanceId subjectId,
            int damage)
        {
            if (cues == null || damage <= 0 || !subjectId.IsValid)
            {
                return;
            }

            cues.Add(new PresentationCue(
                PresentationCueKind.PlayStatFloatingText,
                subjectId: subjectId,
                statFeedbackKind: StatFeedbackKind.Damage,
                statAmount: damage));
        }

        public static void AppendDamageFloatingText(
            IList<PresentationCue> cues,
            HeroInstanceId subjectHeroId,
            int damage)
        {
            if (cues == null || damage <= 0 || !subjectHeroId.IsValid)
            {
                return;
            }

            cues.Add(new PresentationCue(
                PresentationCueKind.PlayStatFloatingText,
                subjectHeroId: subjectHeroId,
                statFeedbackKind: StatFeedbackKind.Damage,
                statAmount: damage));
        }
    }
}
