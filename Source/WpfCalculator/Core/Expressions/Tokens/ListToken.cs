using System.Collections.Generic;
using static WpfCalculator.Expressions.ExpressionTokenizer;

namespace WpfCalculator.Expressions.Tokens
{
    public class ListToken : CollectionToken
    {
        public static readonly string ListStartEndPair =
            ListOpeningChar.ToString() + ListClosingChar.ToString();

        public ListToken(List<Token> tokens) : base(TokenType.List, tokens)
        {
        }

        public ListToken() : this(new List<Token>())
        {
        }

        public override Token Clone()
        {
            return new ListToken(new List<Token>(Children));
        }
    }
}