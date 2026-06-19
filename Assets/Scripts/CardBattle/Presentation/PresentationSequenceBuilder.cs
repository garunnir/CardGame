using System.Collections.Generic;
using CardGame.CardBattle.Cards;

namespace CardGame.CardBattle.Presentation
{
    public static class PresentationSequenceBuilder
    {
        public static PresentationSequence BuildAttack(PresentationContext context, float tailDelay = 0.55f)
        {
            var cues = new List<PresentationCue>();
            if (context?.Behavior == null)
            {
                return new PresentationSequence(cues);
            }

            var collector = new PresentationModuleCollector();
            PresentationModuleFactory.CollectModules(context.Behavior, collector);

            for (var i = 0; i < collector.Modules.Count; i++)
            {
                collector.Modules[i].CollectCues(context, cues);
            }

            DeathPresentationPlanner.AppendDeathCues(context, cues);

            if (tailDelay > 0f)
            {
                cues.Add(new PresentationCue(PresentationCueKind.Wait, tailDelay));
            }

            return new PresentationSequence(cues);
        }

        public static PresentationSequence BuildTurnStartHeal(IReadOnlyList<TurnStartHealEvent> healEvents)
        {
            var cues = new List<PresentationCue>();
            if (healEvents == null || healEvents.Count == 0)
            {
                return new PresentationSequence(cues);
            }

            cues.Add(new PresentationCue(PresentationCueKind.UiHealerBloom));

            for (var i = 0; i < healEvents.Count; i++)
            {
                var healEvent = healEvents[i];
                var healer = healEvent.Healer;
                var target = healEvent.Target;
                if (healer == null || target == null)
                {
                    continue;
                }

                cues.Add(new PresentationCue(
                    PresentationCueKind.PlayHealOnTargetPresentation,
                    subjectId: target.InstanceId,
                    sourceId: healer.InstanceId));

                cues.Add(new PresentationCue(
                    PresentationCueKind.HpBarTween,
                    subjectId: target.InstanceId,
                    hpFrom: healEvent.FromHp,
                    hpTo: healEvent.ToHp));
            }

            return new PresentationSequence(cues);
        }
    }
}
