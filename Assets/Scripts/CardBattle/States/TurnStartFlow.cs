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
            var healEvents = TurnStartHealEffect.Apply(battlefield);
            await context.SyncAllViewsAsync();

            if (!context.IsStateGenerationCurrent(generation))
            {
                return;
            }

            context.RaiseTurnBanner(isPlayerTurn);
            context.RaiseHealerPulse(healEvents);
        }
    }
}
