using System.Collections.Generic;
using Miniräknare.Expressions.Tokens;

namespace Miniräknare.Expressions
{
    public interface IExpressionTree
    {
        public ExpressionOptions Options { get; }
        public IReadOnlyList<Token> Tokens { get; }
    }
}
