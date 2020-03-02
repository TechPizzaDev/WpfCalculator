using System.Collections.Generic;
using Miniräknare.Expressions.Tokens;

namespace Miniräknare.Expressions
{
    public interface IExpressionTree
    {
        public ExpressionOptions ExpressionOptions { get; }
        public IReadOnlyList<Token> Tokens { get; }
    }
}
