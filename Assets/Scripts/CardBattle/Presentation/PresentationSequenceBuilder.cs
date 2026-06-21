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

            cues.Add(new PresentationCue(PresentationCueKind.UiHealerBloom));

            for (var i = 0; i < events.Count; i++)
            {
                var effectEvent = events[i];
                if (effectEvent.Source == null)
                {
                    continue;
                }

                if (effectEvent.TargetCard != null)
                {
                    var target = effectEvent.TargetCard;
                    cues.Add(new PresentationCue(
                        PresentationCueKind.PlayHealOnTargetPresentation,
                        subjectId: target.InstanceId,
                        sourceId: effectEvent.Source.InstanceId));

                    if (effectEvent.Kind == TurnStartStatKind.Heal)
                    {
                        cues.Add(new PresentationCue(
                            PresentationCueKind.HpBarTween,
                            subjectId: target.InstanceId,
                            hpFrom: effectEvent.FromValue,
                            hpTo: effectEvent.ToValue));
                    }
                }
                else if (effectEvent.TargetHero != null)
                {
                    var hero = effectEvent.TargetHero;
                    cues.Add(new PresentationCue(
                        PresentationCueKind.PlayHeroSupportFromSlot,
                        subjectId: effectEvent.Source.InstanceId,
                        subjectHeroId: hero.InstanceId,
                        isMpGain: effectEvent.Kind == TurnStartStatKind.MpGain));

                    if (effectEvent.Kind == TurnStartStatKind.Heal)
                    {
                        cues.Add(new PresentationCue(
                            PresentationCueKind.HeroStatTween,
                            subjectHeroId: hero.InstanceId,
                            hpFrom: effectEvent.FromValue,
                            hpTo: effectEvent.ToValue));
                    }
                    else
                    {
                        cues.Add(new PresentationCue(
                            PresentationCueKind.HeroStatTween,
                            subjectHeroId: hero.InstanceId,
                            mpFrom: effectEvent.FromValue,
                            mpTo: effectEvent.ToValue,
                            isMpGain: true));
                    }
                }
            }

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
