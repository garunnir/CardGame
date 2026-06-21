using CardGame.CardBattle.Core;

namespace CardGame.CardBattle.States
{
    internal static class TurnActionResolver
    {
        public static TurnActionEvent Resolve(IBattleContext context, bool isPlayerTurn)
        {
            var field = context.Field;

            if (isPlayerTurn
                && field.IsCardPoolExhausted(false)
                && field.HasTargetableCardOnBattlefield(true))
            {
                return new TurnActionEvent(true, TurnActionKind.CardAttack);
            }

            if (field.CanTeamAttack(isPlayerTurn))
            {
                return new TurnActionEvent(isPlayerTurn, TurnActionKind.CardAttack);
            }

            var hero = context.HeroArena.GetHero(isPlayerTurn);
            if (field.IsCardPoolExhausted(isPlayerTurn) && hero != null && hero.IsAlive)
            {
                return new TurnActionEvent(isPlayerTurn, TurnActionKind.HeroStrike);
            }

            var message = field.IsCardPoolExhausted(isPlayerTurn)
                ? null
                : "공격 불가 — 턴 스킵";
            return new TurnActionEvent(isPlayerTurn, TurnActionKind.Skip, message);
        }
    }
}
