using System.Collections.Generic;

namespace CardGame.CardBattle.Presentation
{
    public sealed class PresentationSequence
    {
        public PresentationSequence(IReadOnlyList<PresentationCue> cues)
        {
            Cues = cues ?? new List<PresentationCue>();
        }

        public IReadOnlyList<PresentationCue> Cues { get; }
    }
}
