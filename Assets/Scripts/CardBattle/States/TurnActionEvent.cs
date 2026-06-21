namespace CardGame.CardBattle.States
{
    internal readonly struct TurnActionEvent
    {
        public TurnActionEvent(bool isPlayerTurn, TurnActionKind kind, string skipMessage = null)
        {
            IsPlayerTurn = isPlayerTurn;
            Kind = kind;
            SkipMessage = skipMessage;
        }

        public bool IsPlayerTurn { get; }
        public TurnActionKind Kind { get; }
        public string SkipMessage { get; }

        public bool IsAutomatic => Kind != TurnActionKind.CardAttack;
    }
}
