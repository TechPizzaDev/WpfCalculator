using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Miniräknare.Expressions
{
    public partial class ExpressionTokenizer
    {
        [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
        public class ListToken : Token
        {
            public static readonly string ListStartEndPair = ListStartChar.ToString() + ListEndChar.ToString();

            public List<Token> Tokens { get; }

            public int Count => Tokens.Count;

            public ListToken(List<Token> tokens) : base(TokenType.List, ReadOnlyMemory<char>.Empty)
            {
                Tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
            }

            private string GetDebuggerDisplay()
            {
                string prefix = Type + " (" + Count + ")";
                if (Count > 0)
                {
                    var builder = ToStringCore(false);
                    builder.Insert(0, prefix).Insert(prefix.Length, ": \"").Append('"');
                    return builder.ToString();
                }
                return prefix;
            }

            private StringBuilder ToStringCore(bool includeListStartEnd)
            {
                var builder = new StringBuilder();

                if (includeListStartEnd)
                    builder.Append(ListStartChar);

                foreach (var token in Tokens)
                    builder.Append(token.ToString());

                if (includeListStartEnd)
                    builder.Append(ListEndChar);

                return builder;
            }

            public override string ToString()
            {
                if (Tokens.Count == 0)
                    return ListStartEndPair;
                return ToStringCore(true).ToString();
            }
        }
    }
}
