using System;
using System.Collections.Generic;
using Miniräknare.Expressions.Tokens;

namespace Miniräknare.Expressions
{
    public class ExpressionTree : IExpressionTree
    {
        public ExpressionOptions ExpressionOptions { get; }
        public List<Token> Tokens { get; }

        IReadOnlyList<Token> IExpressionTree.Tokens => Tokens;

        public ExpressionTree(ExpressionOptions options, List<Token> tokens)
        {
            ExpressionOptions = options ?? throw new ArgumentNullException(nameof(options));
            Tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        }

        public ExpressionTree(ExpressionOptions options) : this(options, new List<Token>())
        {
        }
    }
}
