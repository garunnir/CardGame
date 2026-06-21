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

            AppendFloatingText(cues, StatFeedbackKind.Damage, damage, subjectId: subjectId);
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

            AppendFloatingText(cues, StatFeedbackKind.Damage, damage, subjectHeroId: subjectHeroId);
        }

        private static void AppendFloatingText(
            IList<PresentationCue> cues,
            StatFeedbackKind kind,
            int amount,
            CardInstanceId subjectId = default,
            HeroInstanceId subjectHeroId = default)
        {
            cues.Add(new PresentationCue(
                PresentationCueKind.PlayStatFloatingText,
                subjectId: subjectId,
                subjectHeroId: subjectHeroId,
                statFeedbackKind: kind,
                statAmount: amount));
        }
    }
}
