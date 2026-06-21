using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Core;
using Cysharp.Threading.Tasks;

namespace CardGame.CardBattle.States
{
    internal static class TurnFlow
    {
        public static async UniTask<TurnActionEvent?> RunStartAndDetectAsync(
            IBattleContext context,
            CardModel[] battlefield,
            bool isPlayerTurn,
            int generation)
        {
            await TurnStartFlow.RunAsync(context, battlefield, isPlayerTurn, generation);

            if (!context.IsStateGenerationCurrent(generation))
            {
                return null;
            }

            return TurnActionFlow.Detect(context, isPlayerTurn);
        }
    }
}
