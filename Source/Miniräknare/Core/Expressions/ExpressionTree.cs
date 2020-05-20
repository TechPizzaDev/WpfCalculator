using System;
using System.Collections.Generic;
using Miniräknare.Expressions.Tokens;

namespace Miniräknare.Expressions
{
    public class ExpressionTree : IExpressionTree
    {
        public ExpressionOptions Options { get; }
        public List<Token> Tokens { get; }
        public ListToken MetaList { get; }

        IReadOnlyList<Token> IExpressionTree.Tokens => Tokens;

        public ExpressionTree(ExpressionOptions options, List<Token> tokens)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            Tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
            MetaList = new ListToken(Tokens);
        }

        public ExpressionTree(ExpressionOptions options) : this(options, new List<Token>())
        {
        }

        public ExpressionTree Clone()
        {
            return new ExpressionTree(Options, new List<Token>(Tokens));
        }

        public override string ToString()
        {
            return MetaList.ToString();
        }
    }
}
