using System;
using CardGame.CardBattle.Core;
using Cysharp.Threading.Tasks;

namespace CardGame.CardBattle.States
{
    internal static class TurnActionFlow
    {
        public static TurnActionEvent Detect(IBattleContext context, bool isPlayerTurn)
        {
            return TurnActionResolver.Resolve(context, isPlayerTurn);
        }

        public static async UniTask<bool> RunAutomaticAsync(
            IBattleContext context,
            TurnActionEvent turnAction,
            Func<bool> isTransitionCurrent)
        {
            if (!isTransitionCurrent())
            {
                return false;
            }

            switch (turnAction.Kind)
            {
                case TurnActionKind.HeroStrike:
                    return await context.ExecuteHeroStrikeTurnAsync(turnAction.IsPlayerTurn);

                case TurnActionKind.Skip:
                    var message = turnAction.SkipMessage;
                    if (!string.IsNullOrEmpty(message))
                    {
                        context.RaiseSkipBanner(message);
                        await UniTask.Delay(TimeSpan.FromSeconds(0.8f));
                    }

                    return isTransitionCurrent();

                default:
                    return true;
            }
        }
    }
}
