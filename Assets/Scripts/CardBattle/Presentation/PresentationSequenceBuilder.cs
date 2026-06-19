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

            var seenHealers = new HashSet<CardModel>();
            for (var i = 0; i < healEvents.Count; i++)
            {
                var healer = healEvents[i].Healer;
                if (healer == null || !seenHealers.Add(healer))
                {
                    continue;
                }

                cues.Add(new PresentationCue(
                    PresentationCueKind.PlayTurnHealPresentation,
                    subjectId: healer.InstanceId));
            }

            for (var i = 0; i < healEvents.Count; i++)
            {
                var healEvent = healEvents[i];
                if (healEvent.Target == null)
                {
                    continue;
                }

                cues.Add(new PresentationCue(
                    PresentationCueKind.HpBarTween,
                    subjectId: healEvent.Target.InstanceId,
                    hpFrom: healEvent.FromHp,
                    hpTo: healEvent.ToHp));
            }

            return new PresentationSequence(cues);
        }
    }
}
