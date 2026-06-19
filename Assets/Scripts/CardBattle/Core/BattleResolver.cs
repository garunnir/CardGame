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
            SecondaryStrikeResult secondary)
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
        public SecondaryStrikeResult Secondary { get; }
    }

    /// <summary>PDF 수식 그대로 공격/반격 연산.</summary>
    public static class BattleResolver
    {
        public static AttackResolution Plan(
            CardModel attacker,
            CardModel target,
            CardModel[] enemyBattlefield)
        {
            if (attacker == null || target == null || !attacker.IsAlive || !target.IsAlive)
            {
                return default;
            }

            var context = new AttackContext(attacker, target, enemyBattlefield, attacker.Behavior);
            return AttackModuleCollector.Plan(context);
        }

        public static AttackOutcome PlanOutcome(BattleActionRequest request, CardModel[] enemyBattlefield)
        {
            var attacker = request.Attacker;
            var target = request.Target;
            if (attacker == null || target == null || !attacker.IsAlive || !target.IsAlive)
            {
                return default;
            }

            var beforeAttackerHp = attacker.CurrentHp;
            var beforeTargetHp = target.CurrentHp;
            var resolution = Plan(attacker, target, enemyBattlefield);

            CardModel lethalTarget = null;
            CardModel lethalAttacker = null;
            CardModel lethalSecondary = null;

            if (beforeTargetHp <= resolution.PrimaryDamage)
            {
                lethalTarget = target;
            }
            else if (resolution.CounterDamage > 0 && beforeAttackerHp <= resolution.CounterDamage)
            {
                lethalAttacker = attacker;
            }

            var secondary = resolution.Secondary;
            var beforeSecondaryHp = secondary.HasTarget ? secondary.Target.CurrentHp : 0;
            if (secondary.HasTarget && secondary.Target.CurrentHp <= secondary.Damage)
            {
                lethalSecondary = secondary.Target;
            }

            return new AttackOutcome(
                resolution,
                beforeAttackerHp,
                beforeTargetHp,
                beforeSecondaryHp,
                lethalTarget,
                lethalAttacker,
                lethalSecondary);
        }

        public static BattleActionResult Apply(AttackResolution resolution, CardModel attacker, CardModel target)
        {
            if (attacker == null || target == null)
            {
                return default;
            }

            target.ApplyDamage(resolution.PrimaryDamage);

            var counterDamage = 0;
            if (resolution.CounterDamage > 0 && target.IsAlive)
            {
                counterDamage = resolution.CounterDamage;
                attacker.ApplyDamage(counterDamage);
            }

            if (resolution.Secondary.HasTarget)
            {
                resolution.Secondary.Target.ApplyDamage(resolution.Secondary.Damage);
            }

            return new BattleActionResult(
                attacker,
                target,
                resolution.PrimaryDamage,
                counterDamage,
                resolution.Secondary);
        }
    }
}

