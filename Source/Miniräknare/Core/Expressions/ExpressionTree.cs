using System;
using System.Collections.Generic;
using Miniräknare.Expressions.Tokens;

namespace Miniräknare.Expressions
{
    public class ExpressionTree : IExpressionTree
    {
        public ExpressionOptions ExpressionOptions { get; }
        public ListToken Tokens { get; }

        IReadOnlyList<Token> IExpressionTree.Tokens => Tokens.Children;

        public ExpressionTree(ExpressionOptions options, ListToken tokens)
        {
            ExpressionOptions = options ?? throw new ArgumentNullException(nameof(options));
            Tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        }

        public ExpressionTree(ExpressionOptions options) : this(options, new ListToken())
        {
        }
    }
}
