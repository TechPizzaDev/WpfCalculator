using System;

namespace Miniräknare
{
    public readonly struct ReadOnlyString : IEquatable<ReadOnlyString>
    {
        public static ReadOnlyString Empty => new ReadOnlyString();

        public ReadOnlyMemory<char> Chars { get; }

        public ReadOnlySpan<char> Span => Chars.Span;

        public bool IsEmpty => Chars.IsEmpty;

        public ReadOnlyString(ReadOnlyMemory<char> value)
        {
            Chars = value;
        }

        public bool Equals(ReadOnlyString other)
        {
            return Chars.Span.SequenceEqual(other.Chars.Span);
        }

        public override bool Equals(object obj)
        {
            return obj is ReadOnlyString str ? Equals(str) : false;
        }

        public override string ToString()
        {
            return Chars.ToString();
        }

        public override int GetHashCode()
        {
            var span = Chars.Span;
            int hash = 17;
            for (int i = 0; i < span.Length; i++)
                hash = hash * 31 + span[i];
            return hash;
        }

        public static implicit operator ReadOnlyString(string value)
        {
            return new ReadOnlyString(value.AsMemory());
        }

        public static implicit operator ReadOnlyString(Memory<char> value)
        {
            return new ReadOnlyString(value);
        }

        public static implicit operator ReadOnlyString(ReadOnlyMemory<char> value)
        {
            return new ReadOnlyString(value);
        }
    }
}