using CardGame.CardBattle.Core;

namespace CardGame.CardBattle.Cards
{
    /// <summary>전장 타겟·공격자 판정. BattleField 배치가 유일한 SSOT.</summary>
    public static class CardTargetingRules
    {
        public static bool IsFaceUpBattlefieldCard(BattleField field, CardModel model)
        {
            return field != null && field.IsTargetableOnBattlefield(model);
        }

        public static bool CanBeginPlayerDrag(BattleField field, CardModel model)
        {
            return model != null
                && model.IsPlayerTeam
                && IsFaceUpBattlefieldCard(field, model);
        }

        public static bool CanAcceptBattlefieldTarget(BattleField field, CardModel model)
        {
            return IsFaceUpBattlefieldCard(field, model);
        }

        public static bool CanTargetEnemyHero(BattleField field, HeroArenaField heroArena, CardModel attacker)
        {
            return attacker != null
                && attacker.IsAlive
                && attacker.IsPlayerTeam
                && field != null
                && field.IsTargetableOnBattlefield(attacker)
                && field.IsCardPoolExhausted(false)
                && heroArena?.EnemyHero != null
                && heroArena.EnemyHero.IsAlive;
        }

        public static bool CanTargetPlayerHero(BattleField field, HeroArenaField heroArena, CardModel attacker)
        {
            return attacker != null
                && attacker.IsAlive
                && !attacker.IsPlayerTeam
                && field != null
                && field.IsTargetableOnBattlefield(attacker)
                && field.IsCardPoolExhausted(true)
                && heroArena?.PlayerHero != null
                && heroArena.PlayerHero.IsAlive;
        }
    }
}
