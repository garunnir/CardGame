namespace CardGame.CardBattle.Cards
{
    /// <summary>카드 공격/특수 효과 전략 인터페이스.</summary>
    public interface ICardEffect
    {
        CardType CardType { get; }

        /// <summary>주 대상에게 가할 피해량.</summary>
        int CalculatePrimaryDamage(CardModel attacker, CardModel target);

        /// <summary>반격 피해를 받는지 여부.</summary>
        bool ReceivesCounterAttack(CardModel attacker, CardModel target);

        /// <summary>반격 피해량 (반격 가능할 때).</summary>
        int CalculateCounterDamage(CardModel attacker, CardModel target);

        /// <summary>무쌍 등 부가 피해. 없으면 null.</summary>
        MusouSecondaryResult TryGetSecondaryDamage(
            CardModel attacker,
            CardModel primaryTarget,
            CardModel[] enemyBattlefield);
    }

    public readonly struct MusouSecondaryResult
    {
        public MusouSecondaryResult(CardModel target, int damage)
        {
            Target = target;
            Damage = damage;
        }

        public CardModel Target { get; }
        public int Damage { get; }
        public bool HasTarget => Target != null && Damage > 0;
    }
}
