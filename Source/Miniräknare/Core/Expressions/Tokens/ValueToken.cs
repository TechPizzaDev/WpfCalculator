using System;
using System.Diagnostics;

namespace Miniräknare.Expressions.Tokens
{
    [DebuggerDisplay("{Value}", Name = "{Type}")]
    public class ValueToken : Token
    {
        public ReadOnlyMemory<char> Value { get; }

        public ValueToken(TokenType type, ReadOnlyMemory<char> value) : base(type)
        {
            Value = value;
        }

        public bool ValueEqualTo(char value)
        {
            if (Value.Length < 1 || Value.Length > 1)
                return false;
            return Value.Span[0] == value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
