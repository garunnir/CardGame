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
            context.Behavior.CollectPresentationModules(collector);

            for (var i = 0; i < collector.Modules.Count; i++)
            {
                collector.Modules[i].CollectCues(context, cues);
            }

            if (tailDelay > 0f)
            {
                cues.Add(new PresentationCue(PresentationCueKind.Wait, tailDelay));
            }

            return new PresentationSequence(cues);
        }

        public static PresentationSequence BuildTurnStartHeal(CardModel[] battlefield)
        {
            var cues = new List<PresentationCue> { new PresentationCue(PresentationCueKind.UiHealerBloom) };

            if (battlefield == null)
            {
                return new PresentationSequence(cues);
            }

            for (var i = 0; i < battlefield.Length; i++)
            {
                var card = battlefield[i];
                if (card == null || !card.IsAlive || card.CardType != CardType.Healer)
                {
                    continue;
                }

                cues.Add(new PresentationCue(PresentationCueKind.PlayTurnHealPresentation, subject: card));
                break;
            }

            return new PresentationSequence(cues);
        }
    }
}
