using System;
using System.Collections.Generic;

namespace Miniräknare.Expressions
{
    public partial class ExpressionTokenizer
    {
        private static TokenDefinition[] _tokenDefinitions;

        public const char SpaceChar = '_';
        public const char ListStartChar = '(';
        public const char ListEndChar = ')';
        public const char ListSeparatorChar = ';';

        static ExpressionTokenizer()
        {
            static TokenDefinition NewDef(TokenType type, Func<char, bool> predicate, bool isSingular = false)
            {
                return new TokenDefinition(type, predicate, isSingular);
            }

            _tokenDefinitions = new[]
            {
                NewDef(TokenType.Operator, IsOperator),
                NewDef(TokenType.Name, IsName),
                NewDef(TokenType.NumberLiteral, IsNumberLiteral),
                NewDef(TokenType.WhiteSpace, char.IsWhiteSpace),
                NewDef(TokenType.Space, c => c == SpaceChar),
                NewDef(TokenType.ListStart, c => c == ListStartChar, true),
                NewDef(TokenType.ListEnd, c => c == ListEndChar, true),
                NewDef(TokenType.ListSeparator, c => c == ListSeparatorChar, true)
            };
        }

        private class TokenDefinition
        {
            public TokenType Type { get; }
            public Func<char, bool> Predicate { get; }
            public bool IsSingular { get; }

            public TokenDefinition(TokenType type, Func<char, bool> predicate, bool isSingular)
            {
                Type = type;
                Predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
                IsSingular = isSingular;
            }
        }

        public static void TokenizeInput(ReadOnlyMemory<char> inputText, ICollection<Token> output)
        {
            var currentType = TokenType.Unknown;
            int lastOffset = 0;
            int offset = 0;

            void FinishToken()
            {
                int length = offset - lastOffset;
                if (length > 0)
                {
                    var slice = inputText.Slice(lastOffset, length);
                    output.Add(new Token(currentType, slice));
                }
            }

            var span = inputText.Span;
            while (offset < inputText.Length)
            {
                char c = span[offset];

                TokenDefinition definition = null;
                for (int j = 0; j < _tokenDefinitions.Length; j++)
                {
                    var d = _tokenDefinitions[j];
                    if (d.Predicate(c))
                    {
                        definition = d;
                        break;
                    }
                }

                var nextType = definition == null ? TokenType.Unknown : definition.Type;
                if (nextType != currentType || definition.IsSingular)
                {
                    FinishToken();

                    lastOffset = offset;
                    currentType = nextType;
                }
                offset++;
            }

            FinishToken();
        }
    }
}
