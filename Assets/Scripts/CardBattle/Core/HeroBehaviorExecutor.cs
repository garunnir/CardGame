using System;
using System.Collections.Generic;
using CardGame.CardBattle.Cards;

namespace CardGame.CardBattle.Core
{
    public readonly struct HeroStrikeResult
    {
        public HeroStrikeResult(
            HeroModel striker,
            HeroModel defender,
            bool usedShield,
            int damageDealt,
            int shieldGained,
            int mpGained)
        {
            Striker = striker;
            Defender = defender;
            UsedShield = usedShield;
            DamageDealt = damageDealt;
            ShieldGained = shieldGained;
            MpGained = mpGained;
        }

        public HeroModel Striker { get; }
        public HeroModel Defender { get; }
        public bool UsedShield { get; }
        public int DamageDealt { get; }
        public int ShieldGained { get; }
        public int MpGained { get; }
        public bool HasDefender => Defender != null;
    }

    public sealed class HeroBehaviorExecutor
    {
        public HeroStrikeResult Execute(
            HeroModel striker,
            HeroModel defender,
            IReadOnlyList<HeroSupportContribution> strikerContributions,
            IReadOnlyList<HeroSupportContribution> defenderContributions)
        {
            if (striker == null || !striker.IsAlive)
            {
                return default;
            }

            var strikerTotals = SlotSupportAggregator.SumForStrike(strikerContributions);
            var defenderTotals = SlotSupportAggregator.SumForStrike(defenderContributions);

            if (striker.CurrentMp >= striker.MaxMp)
            {
                var shieldBehavior = striker.ShieldBehavior;
                var amount = shieldBehavior != null ? shieldBehavior.shieldAmount : 5;
                striker.AddShield(amount);
                striker.ResetMp();
                return new HeroStrikeResult(striker, defender, true, 0, amount, 0);
            }

            var rawDamage = striker.BaseAttack + strikerTotals.StrikeBonus;
            var damageDealt = 0;
            if (defender != null && defender.IsAlive)
            {
                var mitigated = Math.Max(1, rawDamage - defenderTotals.DefenseBonus);
                damageDealt = mitigated;
                defender.ApplyDamage(mitigated);
            }

            var mpGain = strikerTotals.MpGainOnHeroStrike;
            if (mpGain > 0)
            {
                striker.AddMp(mpGain);
            }

            return new HeroStrikeResult(striker, defender, false, damageDealt, 0, mpGain);
        }
    }
}
