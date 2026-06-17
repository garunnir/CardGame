using CardGame.CardBattle.Core;

namespace CardGame.CardBattle.States
{
    /// <summary>승리/패배 정산 및 UI 활성화.</summary>
    public sealed class GameOverState : BaseState
    {
        private readonly bool playerWin;

        public GameOverState(GameManager context, bool playerWin) : base(context)
        {
            this.playerWin = playerWin;
        }

        public override BattleFlowStateId StateId => BattleFlowStateId.GameOver;

        public override void Enter()
        {
            Context.InputProvider.SetEnabled(false);
            Context.RaiseGameOver(playerWin);
            Context.OnBattleResult?.Invoke(playerWin);
        }
    }
}
