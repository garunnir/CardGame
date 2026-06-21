using System;
using CardGame.CardBattle.Cards;

namespace CardGame.CardBattle.Core
{
    public readonly struct HeroInstanceId : IEquatable<HeroInstanceId>
    {
        private static int nextId = 1;

        public HeroInstanceId(int value)
        {
            Value = value;
        }

        public int Value { get; }
        public bool IsValid => Value > 0;

        public static HeroInstanceId NewId() => new HeroInstanceId(nextId++);

        public bool Equals(HeroInstanceId other) => Value == other.Value;

        public override bool Equals(object obj) => obj is HeroInstanceId other && Equals(other);

        public override int GetHashCode() => Value;

        public static bool operator ==(HeroInstanceId left, HeroInstanceId right) => left.Equals(right);

        public static bool operator !=(HeroInstanceId left, HeroInstanceId right) => !left.Equals(right);
    }

    public sealed class HeroModel
    {
        public HeroModel(HeroDataAsset source, bool isPlayerTeam)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            InstanceId = HeroInstanceId.NewId();
            Data = source;
            HeroId = source.heroId;
            DisplayName = source.displayName;
            MaxHp = source.maxHp;
            MaxMp = source.maxMp;
            BaseAttack = source.baseAttack;
            IsPlayerTeam = isPlayerTeam;
            CurrentHp = source.maxHp;
            CurrentMp = 0;
            CurrentShield = 0;
        }

        public HeroInstanceId InstanceId { get; }
        public HeroDataAsset Data { get; }
        public string HeroId { get; }
        public string DisplayName { get; }
        public int MaxHp { get; }
        public int MaxMp { get; }
        public int BaseAttack { get; }
        public bool IsPlayerTeam { get; }
        public int CurrentHp { get; private set; }
        public int CurrentMp { get; private set; }
        public int CurrentShield { get; private set; }
        public bool IsAlive => CurrentHp > 0;

        public HeroNormalAttackBehaviorAsset NormalAttackBehavior => Data.normalAttackBehavior;
        public HeroShieldBehaviorAsset ShieldBehavior => Data.shieldBehavior;

        public void SetHp(int value)
        {
            CurrentHp = Math.Max(0, Math.Min(value, MaxHp));
        }

        public void Heal(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            SetHp(CurrentHp + amount);
        }

        public void AddMp(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            CurrentMp = Math.Min(MaxMp, CurrentMp + amount);
        }

        public void ResetMp()
        {
            CurrentMp = 0;
        }

        public void AddShield(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            CurrentShield += amount;
        }

        public int ApplyDamage(int rawDamage)
        {
            if (rawDamage <= 0)
            {
                return 0;
            }

            var remaining = rawDamage;
            if (CurrentShield > 0)
            {
                var absorbed = Math.Min(CurrentShield, remaining);
                CurrentShield -= absorbed;
                remaining -= absorbed;
            }

            if (remaining <= 0)
            {
                return rawDamage;
            }

            var applied = Math.Max(1, remaining);
            SetHp(CurrentHp - applied);
            return rawDamage;
        }

        public int ResolveCounterDamage()
        {
            var behavior = NormalAttackBehavior;
            if (behavior != null && behavior.counterDamageOverride > 0)
            {
                return behavior.counterDamageOverride;
            }

            return BaseAttack;
        }
    }
}
