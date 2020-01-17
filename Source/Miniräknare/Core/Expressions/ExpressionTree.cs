using System;
using System.Collections.Generic;
using Miniräknare.Expressions.Tokens;

namespace Miniräknare.Expressions
{
    public class ExpressionTree
    {
        public ExpressionOptions Options { get; }
        public List<Token> Tokens { get; }

        public ExpressionTree(ExpressionOptions options, List<Token> tokens)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            Tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        }

        public ExpressionTree(ExpressionOptions options) : this(options, new List<Token>())
        {
        }
    }
}
