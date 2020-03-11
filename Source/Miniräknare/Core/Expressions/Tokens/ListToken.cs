using System.Collections.Generic;
using static Miniräknare.Expressions.ExpressionTokenizer;

namespace Miniräknare.Expressions.Tokens
{
    public class ListToken : CollectionToken
    {
        public static readonly string ListStartEndPair = ListStartChar.ToString() + ListEndChar.ToString();

        public ListToken(List<Token> tokens) : base(TokenType.List, tokens)
        {
        }
    }
}