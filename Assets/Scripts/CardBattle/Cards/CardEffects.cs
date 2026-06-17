using System;
using UnityEngine;

namespace CardGame.CardBattle.Cards
{
    /// <summary>Normal / Ranged / Musou / Healer 전략 구현.</summary>
    public static class CardEffectRegistry
    {
        private static readonly ICardEffect Normal = new NormalCardEffect();
        private static readonly ICardEffect Ranged = new RangedCardEffect();
        private static readonly ICardEffect Musou = new MusouCardEffect();
        private static readonly ICardEffect Healer = new HealerCardEffect();

        public static ICardEffect Get(CardType type)
        {
            switch (type)
            {
                case CardType.Normal: return Normal;
                case CardType.Ranged: return Ranged;
                case CardType.Musou: return Musou;
                case CardType.Healer: return Healer;
                default:
                    Debug.LogWarning($"Unknown CardType {type}, fallback Normal.");
                    return Normal;
            }
        }
    }

    public sealed class NormalCardEffect : ICardEffect
    {
        public CardType CardType => CardType.Normal;

        public int CalculatePrimaryDamage(CardModel attacker, CardModel target)
        {
            return attacker.AttackPower;
        }

        public bool ReceivesCounterAttack(CardModel attacker, CardModel target)
        {
            return true;
        }

        public int CalculateCounterDamage(CardModel attacker, CardModel target)
        {
            return target.AttackPower;
        }

        public MusouSecondaryResult TryGetSecondaryDamage(
            CardModel attacker,
            CardModel primaryTarget,
            CardModel[] enemyBattlefield)
        {
            return default;
        }
    }

    public sealed class RangedCardEffect : ICardEffect
    {
        public CardType CardType => CardType.Ranged;

        public int CalculatePrimaryDamage(CardModel attacker, CardModel target)
        {
            return attacker.AttackPower;
        }

        public bool ReceivesCounterAttack(CardModel attacker, CardModel target)
        {
            return false;
        }

        public int CalculateCounterDamage(CardModel attacker, CardModel target)
        {
            return 0;
        }

        public MusouSecondaryResult TryGetSecondaryDamage(
            CardModel attacker,
            CardModel primaryTarget,
            CardModel[] enemyBattlefield)
        {
            return default;
        }
    }

    public sealed class MusouCardEffect : ICardEffect
    {
        public CardType CardType => CardType.Musou;

        public int CalculatePrimaryDamage(CardModel attacker, CardModel target)
        {
            // 자신의 현재 HP 100%
            return attacker.AttackPower;
        }

        public bool ReceivesCounterAttack(CardModel attacker, CardModel target)
        {
            return true;
        }

        public int CalculateCounterDamage(CardModel attacker, CardModel target)
        {
            return target.AttackPower;
        }

        public MusouSecondaryResult TryGetSecondaryDamage(
            CardModel attacker,
            CardModel primaryTarget,
            CardModel[] enemyBattlefield)
        {
            if (enemyBattlefield == null || primaryTarget == null)
            {
                return default;
            }

            var slot = primaryTarget.SlotIndex;
            var candidates = new System.Collections.Generic.List<CardModel>(2);

            TryAddAdjacent(enemyBattlefield, slot - 1, candidates);
            TryAddAdjacent(enemyBattlefield, slot + 1, candidates);

            if (candidates.Count == 0)
            {
                return default;
            }

            var pick = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            var damage = Mathf.Max(1, attacker.AttackPower / 2);
            return new MusouSecondaryResult(pick, damage);
        }

        private static void TryAddAdjacent(
            CardModel[] battlefield,
            int index,
            System.Collections.Generic.List<CardModel> list)
        {
            if (index < 0 || index >= battlefield.Length)
            {
                return;
            }

            var card = battlefield[index];
            if (card != null && card.IsAlive)
            {
                list.Add(card);
            }
        }
    }

    public sealed class HealerCardEffect : ICardEffect
    {
        public CardType CardType => CardType.Healer;

        public int CalculatePrimaryDamage(CardModel attacker, CardModel target)
        {
            // 공격은 일반과 동일
            return attacker.AttackPower;
        }

        public bool ReceivesCounterAttack(CardModel attacker, CardModel target)
        {
            return true;
        }

        public int CalculateCounterDamage(CardModel attacker, CardModel target)
        {
            return target.AttackPower;
        }

        public MusouSecondaryResult TryGetSecondaryDamage(
            CardModel attacker,
            CardModel primaryTarget,
            CardModel[] enemyBattlefield)
        {
            return default;
        }

        /// <summary>턴 시작 시 아군(자신 제외) 1 회복.</summary>
        public static void ApplyTurnStartHeal(CardModel[] allyBattlefield)
        {
            if (allyBattlefield == null)
            {
                return;
            }

            for (var i = 0; i < allyBattlefield.Length; i++)
            {
                var card = allyBattlefield[i];
                if (card == null || !card.IsAlive || card.CardType != CardType.Healer)
                {
                    continue;
                }

                for (var j = 0; j < allyBattlefield.Length; j++)
                {
                    if (j == i)
                    {
                        continue;
                    }

                    var ally = allyBattlefield[j];
                    if (ally != null && ally.IsAlive)
                    {
                        ally.Heal(1);
                    }
                }
            }
        }
    }
}
