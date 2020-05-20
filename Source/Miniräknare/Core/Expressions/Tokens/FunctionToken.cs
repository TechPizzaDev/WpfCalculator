using System;
using System.Collections.Generic;
using System.Text;

namespace Miniräknare.Expressions.Tokens
{
    public class FunctionToken : CollectionToken
    {
        public ValueToken Name { get; }

        public int ArgumentCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < Children.Count; i++)
                {
                    if (Children[i].Type == TokenType.ListSeparator)
                        continue;
                    count++;
                }
                return count;
            }
        }

        public FunctionToken(ValueToken name, List<Token> arguments)
            : base(TokenType.Function, arguments)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            if (name.Type != TokenType.Name)
                throw new ArgumentException("Invalid token type.", nameof(name));
        }

        public override StringBuilder ToStringCore(StringBuilder builder, bool enclose)
        {
            builder.Append(Name);
            builder.Append(ExpressionTokenizer.ListOpeningChar);
            for (int i = 0; i < Children.Count; i++)
            {
                var token = Children[i];
                if (token is ListToken listToken)
                    listToken.ToStringCore(builder, false);
                else
                    builder.Append(token.ToString());
            }
            builder.Append(ExpressionTokenizer.ListClosingChar);
            return builder;
        }

        public override Token Clone()
        {
            return new FunctionToken((ValueToken)Name.Clone(), new List<Token>(Children));
        }
    }
}
