using CardGame.CardBattle.Cards;

namespace CardGame.CardBattle.Core
{
    /// <summary>공격 연산 결과 + 연출 전 HP 스냅샷 기준 사망 예측.</summary>
    public readonly struct AttackOutcome
    {
        public AttackOutcome(
            AttackResolution resolution,
            int beforeAttackerHp,
            int beforeTargetHp,
            int beforeSecondaryHp,
            CardModel lethalTarget,
            CardModel lethalAttacker,
            CardModel lethalSecondary)
        {
            Resolution = resolution;
            BeforeAttackerHp = beforeAttackerHp;
            BeforeTargetHp = beforeTargetHp;
            BeforeSecondaryHp = beforeSecondaryHp;
            LethalTarget = lethalTarget;
            LethalAttacker = lethalAttacker;
            LethalSecondary = lethalSecondary;
        }

        public AttackResolution Resolution { get; }
        public int BeforeAttackerHp { get; }
        public int BeforeTargetHp { get; }
        public int BeforeSecondaryHp { get; }
        public CardModel LethalTarget { get; }
        public CardModel LethalAttacker { get; }
        public CardModel LethalSecondary { get; }
    }
}
