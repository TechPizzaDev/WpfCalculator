using System;

namespace Miniräknare.Expressions.Tokens
{
    public class TokenDefinition
    {
        public TokenType Type { get; }
        public Func<char, bool> Predicate { get; }
        public bool IsSingular { get; }

        public TokenDefinition(
            TokenType type, Func<char, bool> predicate, bool isSingular)
        {
            Type = type;
            Predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
            IsSingular = isSingular;
        }
    }
}
