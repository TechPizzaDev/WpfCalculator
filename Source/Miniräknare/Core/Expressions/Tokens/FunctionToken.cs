using System;
using System.Text;

namespace Miniräknare.Expressions.Tokens
{
    public class FunctionToken : CollectionToken
    {
        public ValueToken Name { get; }
        public ListToken Arguments { get; }

        public FunctionToken(ValueToken name, ListToken parameters) : base(TokenType.Function, parameters.Children)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            if (name.Type != TokenType.Name)
                throw new ArgumentException("Invalid token type.", nameof(name));

            Arguments = parameters ?? throw new ArgumentNullException(nameof(parameters));
        }

        protected override StringBuilder ToStringCore()
        {
            var paramsString = base.ToStringCore();
            paramsString.Insert(0, Name);
            return paramsString;
        }
    }
}
