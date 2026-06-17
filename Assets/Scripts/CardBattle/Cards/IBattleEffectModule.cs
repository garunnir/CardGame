namespace CardGame.CardBattle.Cards
{
  public interface IPrimaryDamageModule
  {
    int CalculatePrimaryDamage(AttackContext context);
  }

  public interface ICounterAttackModule
  {
    bool ReceivesCounterAttack(AttackContext context);
    int CalculateCounterDamage(AttackContext context);
  }

  public interface ISecondaryDamageModule
  {
    SecondaryStrikeResult TryGetSecondaryDamage(AttackContext context);
  }

  public interface IAttackModuleCollector
  {
    void AddPrimary(IPrimaryDamageModule module);
    void AddCounter(ICounterAttackModule module);
    void AddSecondary(ISecondaryDamageModule module);
  }

  public sealed class AttackModuleCollector : IAttackModuleCollector
  {
    public IPrimaryDamageModule Primary { get; private set; }
    public ICounterAttackModule Counter { get; private set; }
    public ISecondaryDamageModule Secondary { get; private set; }

    public void AddPrimary(IPrimaryDamageModule module) => Primary = module;
    public void AddCounter(ICounterAttackModule module) => Counter = module;
    public void AddSecondary(ISecondaryDamageModule module) => Secondary = module;

    public static AttackResolution Plan(AttackContext context)
    {
      if (context.Attacker == null
        || context.Target == null
        || !context.Attacker.IsAlive
        || !context.Target.IsAlive
        || context.Behavior == null)
      {
        return default;
      }

      var collector = new AttackModuleCollector();
      context.Behavior.CollectAttackModules(collector);

      var primaryDamage = collector.Primary?.CalculatePrimaryDamage(context) ?? 0;
      var counterDamage = 0;

      if (collector.Counter != null
        && collector.Counter.ReceivesCounterAttack(context)
        && context.Target.CurrentHp > primaryDamage)
      {
        counterDamage = collector.Counter.CalculateCounterDamage(context);
      }

      var secondary = collector.Secondary?.TryGetSecondaryDamage(context) ?? default;
      return new AttackResolution(primaryDamage, counterDamage, secondary);
    }

    public static AttackResolution PlanForAiPreview(AttackContext context)
    {
      if (context.Attacker == null
        || context.Target == null
        || !context.Attacker.IsAlive
        || !context.Target.IsAlive
        || context.Behavior == null)
      {
        return default;
      }

      var collector = new AttackModuleCollector();
      context.Behavior.CollectAttackModules(collector);

      var primaryDamage = collector.Primary?.CalculatePrimaryDamage(context) ?? 0;
      var counterDamage = 0;

      if (collector.Counter != null && collector.Counter.ReceivesCounterAttack(context))
      {
        counterDamage = collector.Counter.CalculateCounterDamage(context);
      }

      var secondary = collector.Secondary?.TryGetSecondaryDamage(context) ?? default;
      return new AttackResolution(primaryDamage, counterDamage, secondary);
    }
  }
}
