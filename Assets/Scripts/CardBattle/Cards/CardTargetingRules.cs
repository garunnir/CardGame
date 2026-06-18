namespace CardGame.CardBattle.Cards
{
    /// <summary>전장 앞면 카드만 타겟·공격자로 쓸 수 있는지 판정.</summary>
    public static class CardTargetingRules
    {
        public static bool IsFaceUpBattlefieldCard(CardModel model, ICardBattleView view)
        {
            if (model == null || !model.IsAlive)
            {
                return false;
            }

            if (view is CardEntity entity)
            {
                return entity.Phase == CardBoardPhase.BattlefieldFaceUp;
            }

            return view != null;
        }
    }
}
