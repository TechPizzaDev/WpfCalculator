using System;

namespace Miniräknare
{
    public partial class ExpressionTokenizer
    {
        public enum TokenType
        {
            Unknown,
            NumberLiteral,
            Name,
            Operator,
            ListSeparator,
            Function,
            ListStart,
            ListEnd,
            WhiteSpace,
            Space
        }

        [System.Diagnostics.DebuggerDisplay("{Type}: {Value}")]
        public readonly struct Token
        {
            public TokenType Type { get; }
            public ReadOnlyMemory<char> Value { get; }

            public Token(TokenType type, ReadOnlyMemory<char> value)
            {
                Type = type;
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

        private static bool IsOperator(char c)
        {
            switch (c)
            {
                case '+':
                case '-':
                case '/':
                case '*':
                case '%':
                case '^':
                    return true;

                default:
                    return false;
            }
        }

        private static bool IsName(char c)
        {
            if (char.IsLetter(c))
                return true;
            return false;
        }

        private static bool IsNumberLiteral(char c)
        {
            if (char.IsNumber(c) ||
                c == ',' ||
                c == '.')
                return true;
            return false;
        }
    }
}
