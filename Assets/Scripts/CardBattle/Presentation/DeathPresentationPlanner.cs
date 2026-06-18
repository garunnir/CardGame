using System.Collections.Generic;
using CardGame.CardBattle.Cards;

namespace CardGame.CardBattle.Presentation
{
    /// <summary>공격 연출 종료 시점에 재생할 사망 큐 — refill 전에 순서대로 삽입.</summary>
    internal static class DeathPresentationPlanner
    {
        public static void AppendDeathCues(PresentationContext context, IList<PresentationCue> cues)
        {
            if (context == null || cues == null)
            {
                return;
            }

            var resolution = context.Resolution;
            var targetLethal = context.BeforeTargetHp <= resolution.PrimaryDamage;

            if (targetLethal)
            {
                cues.Add(new PresentationCue(PresentationCueKind.PlayDeathPresentation, subject: context.Target));
            }
            else if (resolution.CounterDamage > 0
                     && context.BeforeAttackerHp <= resolution.CounterDamage)
            {
                cues.Add(new PresentationCue(PresentationCueKind.PlayDeathPresentation, subject: context.Attacker));
            }

            var secondary = resolution.Secondary;
            if (secondary.HasTarget && secondary.Target.CurrentHp <= secondary.Damage)
            {
                cues.Add(new PresentationCue(PresentationCueKind.PlayDeathPresentation, subject: secondary.Target));
            }
        }
    }
}
