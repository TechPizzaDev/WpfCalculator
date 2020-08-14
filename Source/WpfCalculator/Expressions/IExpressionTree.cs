using System.Collections.Generic;
using WpfCalculator.Expressions.Tokens;

namespace WpfCalculator.Expressions
{
    public interface IExpressionTree
    {
        public ExpressionOptions Options { get; }
        public IReadOnlyList<Token> Tokens { get; }
    }
}
