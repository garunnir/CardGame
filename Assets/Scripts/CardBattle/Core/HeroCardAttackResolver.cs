using System;
using CardGame.CardBattle.Cards;

namespace CardGame.CardBattle.Core
{
    public readonly struct CardHeroAttackOutcome
    {
        public CardHeroAttackOutcome(
            int primaryDamage,
            int counterDamage,
            int beforeHeroHp,
            int beforeAttackerHp,
            int beforeHeroShield)
        {
            PrimaryDamage = primaryDamage;
            CounterDamage = counterDamage;
            BeforeHeroHp = beforeHeroHp;
            BeforeAttackerHp = beforeAttackerHp;
            BeforeHeroShield = beforeHeroShield;
        }

        public int PrimaryDamage { get; }
        public int CounterDamage { get; }
        public int BeforeHeroHp { get; }
        public int BeforeAttackerHp { get; }
        public int BeforeHeroShield { get; }
    }

    public static class HeroCardAttackResolver
    {
        public static CardHeroAttackOutcome Plan(CardModel attacker, HeroModel heroTarget)
        {
            if (attacker == null || heroTarget == null || !attacker.IsAlive || !heroTarget.IsAlive)
            {
                return default;
            }

            var context = new AttackContext(attacker, null, null, attacker.Behavior);
            var primary = AttackModuleCollector.CalculatePrimaryDamage(context);
            var counter = WouldHeroSurvive(heroTarget, primary)
                ? heroTarget.ResolveCounterDamage()
                : 0;

            return new CardHeroAttackOutcome(
                primary,
                counter,
                heroTarget.CurrentHp,
                attacker.CurrentHp,
                heroTarget.CurrentShield);
        }

        public static BattleActionResult Apply(CardHeroAttackOutcome outcome, CardModel attacker, HeroModel heroTarget)
        {
            if (attacker == null || heroTarget == null)
            {
                return default;
            }

            heroTarget.ApplyDamage(outcome.PrimaryDamage);

            var counterDamage = 0;
            if (outcome.CounterDamage > 0 && heroTarget.IsAlive && attacker.IsAlive)
            {
                counterDamage = outcome.CounterDamage;
                attacker.ApplyDamage(counterDamage);
            }

            return new BattleActionResult(
                attacker,
                null,
                heroTarget,
                outcome.PrimaryDamage,
                counterDamage,
                default);
        }

        private static bool WouldHeroSurvive(HeroModel hero, int rawDamage)
        {
            if (rawDamage <= 0)
            {
                return hero.IsAlive;
            }

            var remaining = rawDamage;
            if (hero.CurrentShield > 0)
            {
                remaining -= Math.Min(hero.CurrentShield, remaining);
            }

            if (remaining <= 0)
            {
                return true;
            }

            var applied = Math.Max(1, remaining);
            return hero.CurrentHp - applied > 0;
        }
    }
}
