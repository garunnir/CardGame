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

            // 힐러 최우선
            if (target.CardType == CardType.Healer)
            {
                score += HealerPriorityBonus;
            }

            // 고위험(HP=공격력) 타겟
            score += target.CurrentHp * 10f + HighHpThreatBonus;

            var effect = CardEffectRegistry.Get(attacker.CardType);

            // 저HP 공격자 — 반격 없는 원거리 우선
            if (attacker.CurrentHp <= 2 && target.CardType == CardType.Ranged)
            {
                score += SafeRangedBonus;
            }

            // 길동무: HP 1이면 반격 불가 타겟 또는 일격 처형
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

            // 반격으로 죽는 경우 원거리/저HP 대상 가산
            if (effect.ReceivesCounterAttack(attacker, target))
            {
                var counter = effect.CalculateCounterDamage(attacker, target);
                if (counter >= attacker.CurrentHp && target.CardType == CardType.Ranged)
                {
                    score += SafeRangedBonus;
                }
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
            CardModel[] enemyBattlefield,
            CardModel[] playerBattlefield)
        {
            var list = new List<BattleActionRequest>();

            for (var i = 0; i < enemyBattlefield.Length; i++)
            {
                var attacker = enemyBattlefield[i];
                if (attacker == null || !attacker.IsAlive)
                {
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
