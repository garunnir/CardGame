using System.Collections.Generic;
using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Core;
using CardGame.CardBattle.Presentation;
using Cysharp.Threading.Tasks;

namespace CardGame.CardBattle.States
{
    internal static class TurnStartFlow
    {
        public static async UniTask RunAsync(
            IBattleContext context,
            CardModel[] battlefield,
            bool isPlayerTurn,
            int generation)
        {
            context.IsPlayerTurn = isPlayerTurn;
            await context.SyncAllViewsAsync();

            if (!context.IsStateGenerationCurrent(generation))
            {
                return;
            }

            var healEvents = TurnStartHealEffect.Plan(battlefield);
            TurnStartHealEffect.Apply(healEvents);

            var heroEvents = context.PlanHeroTurnStartEffects(isPlayerTurn);

            if (!context.IsStateGenerationCurrent(generation))
            {
                return;
            }

            context.RaiseTurnBanner(isPlayerTurn);

            var unifiedEvents = new List<TurnStartEffectEvent>();
            unifiedEvents.AddRange(TurnStartEffectAggregator.FromCardHealEvents(healEvents));
            unifiedEvents.AddRange(TurnStartEffectAggregator.FromHeroSupportEvents(heroEvents));
            await context.PlayTurnStartEffectsAsync(unifiedEvents);
        }
    }
}
