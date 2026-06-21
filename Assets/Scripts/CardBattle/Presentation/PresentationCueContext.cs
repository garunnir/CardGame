using System;
using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Core;

namespace CardGame.CardBattle.Presentation
{
    internal sealed class PresentationCueContext
    {
        public BattlePresentationSpec Spec { get; private set; }
        public CardGame.CardBattle.UI.UIManager Ui { get; private set; }
        public CardPresentationService Presentation { get; private set; }
        public ICardViewRegistry ViewRegistry { get; private set; }
        public HeroArenaPresenter HeroPresenter { get; private set; }
        public Func<HeroInstanceId, HeroModel> ResolveHero { get; private set; }

        public bool IsBattle => Spec != null;

        public static PresentationCueContext ForBattle(
            BattlePresentationSpec spec,
            Func<HeroInstanceId, HeroModel> resolveHero)
        {
            return new PresentationCueContext
            {
                Spec = spec,
                Presentation = spec?.Presentation,
                ViewRegistry = spec?.CardViewRegistry,
                HeroPresenter = spec?.HeroPresenter,
                Ui = spec?.Ui,
                ResolveHero = resolveHero,
            };
        }

        public static PresentationCueContext ForTurnStart(
            CardGame.CardBattle.UI.UIManager ui,
            CardPresentationService presentation,
            ICardViewRegistry viewRegistry,
            HeroArenaPresenter heroPresenter)
        {
            return new PresentationCueContext
            {
                Ui = ui,
                Presentation = presentation,
                ViewRegistry = viewRegistry,
                HeroPresenter = heroPresenter,
            };
        }

        public IPresentationTargetView ResolveCueTargetView(PresentationCue cue)
        {
            if (cue.SubjectHeroId.IsValid)
            {
                return HeroPresenter != null
                    ? HeroPresenter.GetPresentationView(cue.SubjectHeroId)
                    : Spec?.GetHeroTargetView(cue.SubjectHeroId);
            }

            if (cue.SubjectId.IsValid)
            {
                return ResolveCardTargetView(cue.SubjectId);
            }

            return null;
        }

        public IPresentationTargetView ResolveCardTargetView(CardInstanceId id)
        {
            if (Spec != null)
            {
                return Spec.GetCardTargetView(id);
            }

            if (ViewRegistry != null && ViewRegistry.TryGetView(id, out var view))
            {
                return new CardPresentationTargetAdapter(view);
            }

            return null;
        }

        public IPresentationTargetView ResolveShakeView(PresentationCue cue)
        {
            if (cue.SubjectHeroId.IsValid)
            {
                return Spec.GetHeroTargetView(cue.SubjectHeroId);
            }

            if (cue.SubjectId.IsValid)
            {
                return Spec.GetCardTargetView(cue.SubjectId);
            }

            if (Spec.PrimaryTargetHero != null)
            {
                return Spec.GetHeroTargetView(Spec.PrimaryTargetHero);
            }

            return Spec.GetCardTargetView(Spec.PrimaryTargetCard?.InstanceId ?? default);
        }
    }
}
