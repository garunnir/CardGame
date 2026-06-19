using System;

namespace CardGame.CardBattle.Core
{
    /// <summary>런타임 카드 인스턴스 식별자. 뷰·연출 큐는 도메인 객체 대신 이 값을 참조.</summary>
    public readonly struct CardInstanceId : IEquatable<CardInstanceId>
    {
        public CardInstanceId(int value)
        {
            Value = value;
        }

        public int Value { get; }

        public bool IsValid => Value != 0;

        public bool Equals(CardInstanceId other) => Value == other.Value;

        public override bool Equals(object obj) => obj is CardInstanceId other && Equals(other);

        public override int GetHashCode() => Value;

        public static bool operator ==(CardInstanceId left, CardInstanceId right) => left.Equals(right);

        public static bool operator !=(CardInstanceId left, CardInstanceId right) => !left.Equals(right);

        public override string ToString() => Value.ToString();
    }
}
