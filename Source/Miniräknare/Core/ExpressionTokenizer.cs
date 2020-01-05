using System;
using System.Collections.Generic;

namespace Miniräknare
{
    public partial class ExpressionTokenizer
    {
        private static TokenDefinition[] _tokenDefinitions;

        static ExpressionTokenizer()
        {
            var tokenDefinitions = new List<TokenDefinition>();

            void Add(TokenType type, Func<char, bool> predicate, bool isSingular = false)
            {
                tokenDefinitions.Add(new TokenDefinition(type, predicate, isSingular));
            }

            Add(TokenType.Operator, IsOperator);
            Add(TokenType.Name, IsName);
            Add(TokenType.NumberLiteral, IsNumberLiteral);
            Add(TokenType.WhiteSpace, char.IsWhiteSpace);
            Add(TokenType.Space, c => c == '_');
            Add(TokenType.ListStart, c => c == '(', true);
            Add(TokenType.ListEnd, c => c == ')', true);
            Add(TokenType.ListSeparator, c => c == ';', true);

            _tokenDefinitions = tokenDefinitions.ToArray();
        }

        private readonly struct TokenDefinition
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

                TokenDefinition definition = default;
                for (int j = 0; j < _tokenDefinitions.Length; j++)
                {
                    definition = _tokenDefinitions[j];
                    if (definition.Predicate(c))
                        break;
                    definition = default;
                }

                // "definition" will be 'default' if none was found.
                var nextType = definition.Predicate == null ? TokenType.Unknown : definition.Type;
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
