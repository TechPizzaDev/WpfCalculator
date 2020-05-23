using System.Diagnostics;

namespace WpfCalculator.Expressions.Tokens
{
    // TODO: add debugging info containing source map

    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ", nq}")]
    public abstract class Token
    {
        public CollectionToken Parent { get; set; }
        public TokenType Type { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal virtual string DebuggerDisplay => Type.ToString();

        public Token(TokenType type)
        {
            Type = type;
        }

        public abstract Token Clone();

        public virtual bool IsOrContains(Token other)
        {
            return this == other;
        }
    }
}
