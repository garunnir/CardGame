using System;
using CardGame.CardBattle.Cards;

namespace CardGame.CardBattle.Core
{
    public readonly struct BattleActionRequest
    {
        public BattleActionRequest(CardModel attacker, CardModel target)
        {
            Attacker = attacker;
            Target = target;
        }

        public CardModel Attacker { get; }
        public CardModel Target { get; }
    }

    public readonly struct BattleActionResult
    {
        public BattleActionResult(
            CardModel attacker,
            CardModel primaryTarget,
            int primaryDamage,
            int counterDamage,
            MusouSecondaryResult secondary)
        {
            Attacker = attacker;
            PrimaryTarget = primaryTarget;
            PrimaryDamage = primaryDamage;
            CounterDamage = counterDamage;
            Secondary = secondary;
        }

        public CardModel Attacker { get; }
        public CardModel PrimaryTarget { get; }
        public int PrimaryDamage { get; }
        public int CounterDamage { get; }
        public MusouSecondaryResult Secondary { get; }
    }

    /// <summary>PDF 수식 그대로 공격/반격 연산.</summary>
    public static class BattleResolver
    {
        public static BattleActionResult Resolve(
            CardModel attacker,
            CardModel target,
            CardModel[] enemyBattlefield)
        {
            if (attacker == null || target == null || !attacker.IsAlive || !target.IsAlive)
            {
                return default;
            }

            var effect = CardEffectRegistry.Get(attacker.CardType);
            var primaryDamage = effect.CalculatePrimaryDamage(attacker, target);
            var counterDamage = 0;

            target.ApplyDamage(primaryDamage);

            if (effect.ReceivesCounterAttack(attacker, target) && target.IsAlive)
            {
                counterDamage = effect.CalculateCounterDamage(attacker, target);
                attacker.ApplyDamage(counterDamage);
            }

            var secondary = effect.TryGetSecondaryDamage(attacker, target, enemyBattlefield);
            if (secondary.HasTarget)
            {
                secondary.Target.ApplyDamage(secondary.Damage);
            }

            return new BattleActionResult(
                attacker,
                target,
                primaryDamage,
                counterDamage,
                secondary);
        }
    }
}
