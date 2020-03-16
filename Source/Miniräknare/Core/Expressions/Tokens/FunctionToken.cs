using System;
using System.Text;

namespace Miniräknare.Expressions.Tokens
{
    public class FunctionToken : CollectionToken
    {
        public ValueToken Name { get; }
        public ListToken ArgumentList { get; }

        public int ArgumentCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < ArgumentList.Count; i++)
                {
                    if (ArgumentList[i].Type == TokenType.ListSeparator)
                        continue;
                    count++;
                }
                return count;
            }
        }

        public FunctionToken(ValueToken name, ListToken arguments) 
            : base(TokenType.Function, arguments.Children)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            if (name.Type != TokenType.Name)
                throw new ArgumentException("Invalid token type.", nameof(name));

            ArgumentList = arguments ?? throw new ArgumentNullException(nameof(arguments));
        }

        protected override StringBuilder ToStringCore(StringBuilder builder)
        {
            builder.Append(Name);
            builder.Append(ExpressionTokenizer.ListStartChar);
            base.ToStringCore(builder);
            builder.Append(ExpressionTokenizer.ListEndChar);
            return builder;
        }
    }
}
