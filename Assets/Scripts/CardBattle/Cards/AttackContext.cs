namespace CardGame.CardBattle.Cards
{
  public readonly struct AttackContext
  {
    public AttackContext(
      CardModel attacker,
      CardModel target,
      CardModel[] enemyBattlefield,
      CardBehaviorAsset behavior)
    {
      Attacker = attacker;
      Target = target;
      EnemyBattlefield = enemyBattlefield;
      Behavior = behavior;
    }

    public CardModel Attacker { get; }
    public CardModel Target { get; }
    public CardModel[] EnemyBattlefield { get; }
    public CardBehaviorAsset Behavior { get; }
  }

  public readonly struct SecondaryStrikeResult
  {
    public SecondaryStrikeResult(CardModel target, int damage)
    {
      Target = target;
      Damage = damage;
    }

    public CardModel Target { get; }
    public int Damage { get; }
    public bool HasTarget => Target != null && Damage > 0;
  }
}
