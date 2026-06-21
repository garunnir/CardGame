using System.Collections.Generic;
using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Core;

namespace CardGame.CardBattle.Presentation
{
    public static class PresentationSequenceBuilder
    {
        public static PresentationSequence Build(BattlePresentationSpec spec)
        {
            var cues = new List<PresentationCue>();
            if (spec == null)
            {
                return new PresentationSequence(cues);
            }

            var collector = new PresentationModuleCollector();
            PresentationModuleFactory.CollectModules(spec, collector);

            for (var i = 0; i < collector.Modules.Count; i++)
            {
                collector.Modules[i].CollectCues(spec, cues);
            }

            DeathPresentationPlanner.AppendDeathCues(spec, cues);

            if (spec.TailDelay > 0f)
            {
                cues.Add(new PresentationCue(PresentationCueKind.Wait, spec.TailDelay));
            }

            return new PresentationSequence(cues);
        }

        public static PresentationSequence BuildAttack(PresentationContext context, float tailDelay = 0.55f)
        {
            if (context == null)
            {
                return new PresentationSequence(new List<PresentationCue>());
            }

            var spec = PresentationContext.ToSpec(context, tailDelay);
            return Build(spec);
        }

        public static PresentationSequence BuildTurnStartEffects(IReadOnlyList<TurnStartEffectEvent> events)
        {
            var cues = new List<PresentationCue>();
            if (events == null || events.Count == 0)
            {
                return new PresentationSequence(cues);
            }

            var planInputs = TurnStartPresentationPlanInput.FromEvents(events);
            TurnStartPresentationPlanner.AppendCues(planInputs, cues);
            return new PresentationSequence(cues);
        }

        public static PresentationSequence BuildTurnStartHeal(IReadOnlyList<TurnStartHealEvent> healEvents)
        {
            if (healEvents == null || healEvents.Count == 0)
            {
                return new PresentationSequence(new List<PresentationCue>());
            }

            var unified = TurnStartEffectAggregator.FromCardHealEvents(healEvents);
            return BuildTurnStartEffects(unified);
        }
    }
}
