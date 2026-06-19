namespace CardGame.CardBattle.Core
{
    public enum BattleFlowStateId
    {
        Init = 0,
        PlayerTurn = 1,
        EnemyTurn = 2,
        Battle = 3,
        GameOver = 4
    }

    /// <summary>상태 패턴 추상 베이스.</summary>
    public abstract class BaseState
    {
        protected BaseState(IBattleContext context)
        {
            Context = context;
        }

        protected IBattleContext Context { get; }

        public abstract BattleFlowStateId StateId { get; }

        public virtual void Enter()
        {
        }

        public virtual void Exit()
        {
        }

        public virtual void Tick()
        {
        }
    }
}
