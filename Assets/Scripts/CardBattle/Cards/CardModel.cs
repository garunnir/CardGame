using System;

namespace CardGame.CardBattle.Cards
{
    /// <summary>런타임 카드 인스턴스. CardDataAsset에서 복사 생성.</summary>
    public sealed class CardModel
    {
        public CardModel(CardDataAsset source, bool isPlayerTeam, int slotIndex)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            Data = source;
            CardId = source.cardId;
            DisplayName = source.displayName;
            CardType = source.cardType;
            MaxHp = source.maxHp;
            CurrentHp = source.maxHp;
            IsPlayerTeam = isPlayerTeam;
            SlotIndex = slotIndex;
        }

        public CardDataAsset Data { get; }
        public string CardId { get; }
        public string DisplayName { get; }
        public CardType CardType { get; }
        public int MaxHp { get; }
        public int CurrentHp { get; private set; }
        public bool IsPlayerTeam { get; }
        public int SlotIndex { get; set; }
        public bool IsAlive => CurrentHp > 0;

        /// <summary>현재 HP가 곧 공격력.</summary>
        public int AttackPower => CurrentHp;

        public void SetHp(int value)
        {
            CurrentHp = Math.Max(0, Math.Min(value, MaxHp));
        }

        public void ApplyDamage(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            SetHp(CurrentHp - amount);
        }

        public void Heal(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            SetHp(CurrentHp + amount);
        }

        public CardModel CloneForRuntime(bool isPlayerTeam, int slotIndex)
        {
            var clone = new CardModel(Data, isPlayerTeam, slotIndex);
            clone.SetHp(CurrentHp);
            return clone;
        }
    }
}
