using System.Collections.Generic;
using static Miniräknare.Expressions.ExpressionTokenizer;

namespace Miniräknare.Expressions.Tokens
{
    public class ListToken : CollectionToken
    {
        public static readonly string ListStartEndPair = ListStartChar.ToString() + ListEndChar.ToString();

        public ListToken(ListToken parent, List<Token> tokens) : base(parent, TokenType.List, tokens)
        {
        }
    }
}