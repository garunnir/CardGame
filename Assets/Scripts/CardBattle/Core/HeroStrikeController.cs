using CardGame.CardBattle.Cards;

namespace CardGame.CardBattle.Core
{
    public sealed class HeroStrikeController
    {
        private readonly HeroBehaviorExecutor executor = new HeroBehaviorExecutor();

        public HeroStrikeResult ExecuteAfterCardAttack(BattleField field, HeroArenaField heroArena, bool attackerIsPlayerTeam)
        {
            if (field == null || heroArena == null)
            {
                return default;
            }

            var striker = heroArena.GetHero(attackerIsPlayerTeam);
            var defender = heroArena.GetOpponentHero(attackerIsPlayerTeam);
            if (striker == null || !striker.IsAlive)
            {
                return default;
            }

            var strikerField = field.GetBattlefield(attackerIsPlayerTeam);
            var defenderField = field.GetBattlefield(!attackerIsPlayerTeam);
            var strikerContributions = SlotSupportAggregator.PlanContributions(strikerField);
            var defenderContributions = SlotSupportAggregator.PlanContributions(defenderField);
            return executor.Execute(striker, defender, strikerContributions, defenderContributions);
        }
    }
}
