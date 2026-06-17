namespace CardGame.CardBattle.Cards
{
  public readonly struct AttackResolution
  {
    public AttackResolution(
      int primaryDamage,
      int counterDamage,
      SecondaryStrikeResult secondary)
    {
      PrimaryDamage = primaryDamage;
      CounterDamage = counterDamage;
      Secondary = secondary;
    }

    public int PrimaryDamage { get; }
    public int CounterDamage { get; }
    public SecondaryStrikeResult Secondary { get; }
  }
}
