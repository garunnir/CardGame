using System;
using CardGame.CardBattle.Core;

namespace CardGame.CardBattle.Cards
{
    /// <summary>런타임 카드 인스턴스. CardDataAsset에서 복사 생성.</summary>
    public sealed class CardModel
    {
        private static int nextInstanceId = 1;

        public CardModel(CardDataAsset source, bool isPlayerTeam, int slotIndex)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            InstanceId = new CardInstanceId(nextInstanceId++);
            Data = source;
            CardId = source.cardId;
            DisplayName = source.displayName;
            MaxHp = source.maxHp;
            CurrentHp = source.maxHp;
            IsPlayerTeam = isPlayerTeam;
            SlotIndex = slotIndex;
        }

        public CardInstanceId InstanceId { get; }
        public CardDataAsset Data { get; }
        public string CardId { get; }
        public string DisplayName { get; }
        public int MaxHp { get; }
        public int CurrentHp { get; private set; }
        public bool IsPlayerTeam { get; }
        public int SlotIndex { get; set; }
        public bool IsAlive => CurrentHp > 0;

        public CardBehaviorAsset Behavior => CardBehaviorLibrary.Resolve(Data.behavior);

        /// <summary>행동 SO의 StrategyType. 기존 CardType 호출부 호환.</summary>
        public CardType CardType => Behavior.StrategyType;

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
