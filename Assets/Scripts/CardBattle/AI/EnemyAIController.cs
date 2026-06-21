using System.Collections.Generic;
using CardGame.CardBattle.Cards;
using CardGame.CardBattle.Core;

namespace CardGame.CardBattle.AI
{
    /// <summary>Utility AI — PDF 우선순위 점수 기반 타겟 선정.</summary>
    public static class EnemyAIController
    {
        private const float HealerPriorityBonus = 1000f;
        private const float HighHpThreatBonus = 100f;
        private const float SafeRangedBonus = 250f;
        private const float KamikazeLowHpBonus = 180f;
        private const float FinishLowHpBonus = 200f;

        public static float CalculateTargetScore(CardModel attacker, CardModel target)
        {
            if (attacker == null || target == null || !attacker.IsAlive || !target.IsAlive)
            {
                return float.MinValue;
            }

            var score = 0f;

            if (target.CardType == CardType.Healer)
            {
                score += HealerPriorityBonus;
            }

            score += target.CurrentHp * 10f + HighHpThreatBonus;

            var preview = AttackModuleCollector.PlanForAiPreview(
                new AttackContext(attacker, target, null, attacker.Behavior));

            if (attacker.CurrentHp <= 2 && target.CardType == CardType.Ranged)
            {
                score += SafeRangedBonus;
            }

            if (attacker.CurrentHp <= 1)
            {
                if (target.CardType == CardType.Ranged)
                {
                    score += SafeRangedBonus;
                }

                if (target.CurrentHp <= attacker.CurrentHp)
                {
                    score += KamikazeLowHpBonus + FinishLowHpBonus;
                }
            }

            if (preview.CounterDamage > 0
                && preview.CounterDamage >= attacker.CurrentHp
                && target.CardType == CardType.Ranged)
            {
                score += SafeRangedBonus;
            }

            return score;
        }

        public static CardModel SelectBestTarget(CardModel attacker, CardModel[] playerBattlefield)
        {
            CardModel best = null;
            var bestScore = float.MinValue;

            for (var i = 0; i < playerBattlefield.Length; i++)
            {
                var target = playerBattlefield[i];
                if (target == null || !target.IsAlive)
                {
                    continue;
                }

                var score = CalculateTargetScore(attacker, target);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = target;
                }
            }

            return best;
        }

        public static List<BattleActionRequest> BuildTurnActions(
            BattleField field,
            HeroArenaField heroArena,
            CardModel[] enemyBattlefield,
            CardModel[] playerBattlefield)
        {
            var list = new List<BattleActionRequest>();
            if (field == null)
            {
                return list;
            }

            if (!field.CanTeamAttack(false))
            {
                return list;
            }

            var playerCardsExhausted = field.IsCardPoolExhausted(true);

            var attackerIndices = new List<int>(enemyBattlefield.Length);
            for (var i = 0; i < enemyBattlefield.Length; i++)
            {
                var attacker = enemyBattlefield[i];
                if (attacker != null && attacker.IsAlive)
                {
                    attackerIndices.Add(i);
                }
            }

            attackerIndices.Sort((a, b) =>
            {
                var hpCompare = enemyBattlefield[b].CurrentHp.CompareTo(enemyBattlefield[a].CurrentHp);
                return hpCompare != 0 ? hpCompare : a.CompareTo(b);
            });

            for (var i = 0; i < attackerIndices.Count; i++)
            {
                var attacker = enemyBattlefield[attackerIndices[i]];

                if (playerCardsExhausted
                    && CardTargetingRules.CanTargetPlayerHero(field, heroArena, attacker))
                {
                    list.Add(new BattleActionRequest(attacker, heroArena.PlayerHero));
                    continue;
                }

                var target = SelectBestTarget(attacker, playerBattlefield);
                if (target != null)
                {
                    list.Add(new BattleActionRequest(attacker, target));
                }
            }

            return list;
        }
    }
}
