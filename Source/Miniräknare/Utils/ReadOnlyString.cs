using System;
using System.Diagnostics.CodeAnalysis;

namespace Miniräknare
{
    public readonly struct ReadOnlyString : IEquatable<ReadOnlyString>
    {
        public ReadOnlyMemory<char> Chars { get; }

        public ReadOnlyString(ReadOnlyMemory<char> value)
        {
            Chars = value;
        }

        public bool Equals([AllowNull] ReadOnlyString other)
        {
            return Chars.Span.SequenceEqual(other.Chars.Span);
        }

        public static implicit operator ReadOnlyString(Memory<char> memory)
        {
            return new ReadOnlyString(memory);
        }

        public static implicit operator ReadOnlyString(ReadOnlyMemory<char> memory)
        {
            return new ReadOnlyString(memory);
        }

        public override bool Equals(object obj)
        {
            return obj is ReadOnlyString str ? Equals(str) : false;
        }

        public override int GetHashCode()
        {
            var span = Chars.Span;
            int hash = 17;
            for (int i = 0; i < span.Length; i++)
                hash = hash * 31 + span[i];
            return hash;
        }
    }
}