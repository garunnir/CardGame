using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardGame.CardBattle.Cards
{
  public sealed class PrimaryDamageModule : IPrimaryDamageModule
  {
    private readonly Func<CardModel, int> _scaleDamage;

    public PrimaryDamageModule(Func<CardModel, int> scaleDamage)
    {
      _scaleDamage = scaleDamage ?? throw new ArgumentNullException(nameof(scaleDamage));
    }

    public int CalculatePrimaryDamage(AttackContext context)
    {
      return _scaleDamage(context.Attacker);
    }
  }

  public sealed class CounterAttackModule : ICounterAttackModule
  {
    private readonly bool _receivesCounterAttack;

    public CounterAttackModule(bool receivesCounterAttack)
    {
      _receivesCounterAttack = receivesCounterAttack;
    }

    public bool ReceivesCounterAttack(AttackContext context) => _receivesCounterAttack;

    public int CalculateCounterDamage(AttackContext context) => context.Target.AttackPower;
  }

  public sealed class AdjacentSplashModule : ISecondaryDamageModule
  {
    private readonly MusouBehaviorAsset _behavior;

    public AdjacentSplashModule(MusouBehaviorAsset behavior)
    {
      _behavior = behavior;
    }

    public SecondaryStrikeResult TryGetSecondaryDamage(AttackContext context)
    {
      var primaryTarget = context.Target;
      var enemyBattlefield = context.EnemyBattlefield;
      if (enemyBattlefield == null || primaryTarget == null)
      {
        return default;
      }

      var slot = primaryTarget.SlotIndex;
      var candidates = new List<CardModel>(2);

      TryAddAdjacent(enemyBattlefield, slot - 1, candidates);
      TryAddAdjacent(enemyBattlefield, slot + 1, candidates);

      if (candidates.Count == 0)
      {
        return default;
      }

      var pick = candidates[UnityEngine.Random.Range(0, candidates.Count)];
      var damage = _behavior != null
        ? _behavior.ScaleSecondaryDamage(context.Attacker.AttackPower)
        : BehaviorDamageMath.ScaleSecondary(0.5f, context.Attacker.AttackPower);
      return new SecondaryStrikeResult(pick, damage);
    }

    private static void TryAddAdjacent(
      CardModel[] battlefield,
      int index,
      List<CardModel> list)
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

  public readonly struct TurnStartHealEvent
  {
    public TurnStartHealEvent(CardModel healer, CardModel target, int amount)
    {
      Healer = healer;
      Target = target;
      Amount = amount;
    }

    public CardModel Healer { get; }
    public CardModel Target { get; }
    public int Amount { get; }
  }

  public static class TurnStartHealEffect
  {
    public static IReadOnlyList<TurnStartHealEvent> Apply(CardModel[] battlefield)
    {
      var events = new List<TurnStartHealEvent>();
      if (battlefield == null)
      {
        return events;
      }

      for (var i = 0; i < battlefield.Length; i++)
      {
        var card = battlefield[i];
        if (card == null || !card.IsAlive || card.CardType != CardType.Healer)
        {
          continue;
        }

        var healer = card.Behavior as HealerBehaviorAsset;
        var amount = healer != null ? healer.turnHealAmount : 1;
        var excludesSelf = healer == null || healer.excludesSelf;

        for (var j = 0; j < battlefield.Length; j++)
        {
          if (excludesSelf && j == i)
          {
            continue;
          }

          var ally = battlefield[j];
          if (ally != null && ally.IsAlive)
          {
            ally.Heal(amount);
            events.Add(new TurnStartHealEvent(card, ally, amount));
          }
        }
      }

      return events;
    }
  }
}
