using System.Collections.Generic;
using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Core;

namespace CardGame.CardBattle.Presentation
{
    public readonly struct TurnStartPresentationPlanInput
    {
        public TurnStartPresentationPlanInput(
            CardInstanceId sourceId,
            CardInstanceId targetCardId,
            HeroInstanceId targetHeroId,
            TurnStartStatKind kind,
            int fromValue,
            int toValue,
            ProjectilePresentationAsset projectilePresentation)
        {
            SourceId = sourceId;
            TargetCardId = targetCardId;
            TargetHeroId = targetHeroId;
            Kind = kind;
            FromValue = fromValue;
            ToValue = toValue;
            ProjectilePresentation = projectilePresentation;
        }

        public CardInstanceId SourceId { get; }
        public CardInstanceId TargetCardId { get; }
        public HeroInstanceId TargetHeroId { get; }
        public TurnStartStatKind Kind { get; }
        public int FromValue { get; }
        public int ToValue { get; }
        public ProjectilePresentationAsset ProjectilePresentation { get; }

        public int Delta => ToValue - FromValue;

        public static List<TurnStartPresentationPlanInput> FromEvents(
            IReadOnlyList<TurnStartEffectEvent> events)
        {
            var inputs = new List<TurnStartPresentationPlanInput>();
            if (events == null)
            {
                return inputs;
            }

            for (var i = 0; i < events.Count; i++)
            {
                var effectEvent = events[i];
                if (effectEvent.Kind == TurnStartStatKind.Heal)
                {
                    if (effectEvent.Source == null)
                    {
                        continue;
                    }

                    var projectile = PresentationAssetResolve.ResolveTurnHeal(effectEvent.Source.Behavior);
                    inputs.Add(new TurnStartPresentationPlanInput(
                        effectEvent.Source.InstanceId,
                        effectEvent.TargetCard?.InstanceId ?? default,
                        effectEvent.TargetHero?.InstanceId ?? default,
                        TurnStartStatKind.Heal,
                        effectEvent.FromValue,
                        effectEvent.ToValue,
                        projectile));
                    continue;
                }

                if (effectEvent.Kind == TurnStartStatKind.MpGain)
                {
                    inputs.Add(new TurnStartPresentationPlanInput(
                        effectEvent.Source?.InstanceId ?? default,
                        default,
                        effectEvent.TargetHero?.InstanceId ?? default,
                        TurnStartStatKind.MpGain,
                        effectEvent.FromValue,
                        effectEvent.ToValue,
                        null));
                }
            }

            return inputs;
        }
    }
}
