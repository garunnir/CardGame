using System.Collections.Generic;
using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Core;
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

            if (!context.IsStateGenerationCurrent(generation))
            {
                return;
            }

            context.RaiseTurnBanner(isPlayerTurn);
            await context.PlayTurnStartHealAsync(healEvents);
        }
    }
}
