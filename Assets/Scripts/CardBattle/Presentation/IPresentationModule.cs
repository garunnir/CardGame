using System.Collections.Generic;

namespace CardGame.CardBattle.Presentation
{
    public interface IPresentationModule
    {
        void CollectCues(BattlePresentationSpec spec, IList<PresentationCue> cues);
    }

    public interface IPresentationModuleCollector
    {
        void Add(IPresentationModule module);
    }

    public sealed class PresentationModuleCollector : IPresentationModuleCollector
    {
        private readonly List<IPresentationModule> modules = new List<IPresentationModule>();

        public IReadOnlyList<IPresentationModule> Modules => modules;

        public void Add(IPresentationModule module)
        {
            if (module != null)
            {
                modules.Add(module);
            }
        }
    }
}
