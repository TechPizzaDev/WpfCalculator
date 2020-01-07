using System.Diagnostics;

namespace Miniräknare.Expressions.Tokens
{
    // TODO: add debugging info containing source map

    [DebuggerDisplay("{Type}")]
    public abstract class Token
    {
        public TokenType Type { get; }

        public Token(TokenType type)
        {
            Type = type;
        }
    }
}
