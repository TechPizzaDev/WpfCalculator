using System;
using System.Collections.Generic;
using Miniräknare.Expressions.Tokens;

namespace Miniräknare.Expressions
{
    public class ExpressionTree
    {
        public List<Token> Tokens { get; }

        public ExpressionTree(List<Token> tokens)
        {
            Tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        }

        public ExpressionTree() : this(new List<Token>())
        {
        }
    }
}
