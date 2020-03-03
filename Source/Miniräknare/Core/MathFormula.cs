using System;
using System.Collections.Generic;
using Miniräknare.Expressions;
using Miniräknare.Expressions.Tokens;

namespace Miniräknare
{
    public class MathFormula : IExpressionTree
    {
        private ExpressionTree _tree;
        private List<ValueToken> _inputs;

        public ExpressionParser.ParseCode ParseCode { get; }

        public bool IsValid => ParseCode == ExpressionParser.ParseCode.Ok;

        public ReadOnlyMemory<MathFormula> Permutations { get; }

        #region IExpressionTree

        public ExpressionOptions ExpressionOptions => _tree.ExpressionOptions;
        public IReadOnlyList<Token> Tokens => _tree.Tokens;

        #endregion

        public MathFormula(ExpressionOptions options, ReadOnlyMemory<char> formula)
        {
            var sourceTree = new ExpressionTree(options);
            ExpressionTokenizer.Tokenize(formula, sourceTree.Tokens);

            ParseCode = ExpressionParser.Parse(sourceTree, out var output);
            if (ParseCode != ExpressionParser.ParseCode.Ok)
                return;

            _tree = new ExpressionTree(options, new List<Token>(output));
            _inputs = GatherInputs(_tree);
            Permutations = CreatePermutations();
        }

        public MathFormula(ExpressionOptions options, MathFormulaData data) 
            : this(options, data.Formula.AsMemory())
        {
        }

        private static List<ValueToken> GatherInputs(IExpressionTree tree)
        {
            var inputs = new List<ValueToken>();
            var probe = new ExpressionTreeProbe();
            probe.ProbeReference += (reference) =>
            {
                inputs.Add(reference);
            };
            probe.Probe(tree);
            return inputs;
        }

        private MathFormula[] CreatePermutations()
        {
            var permutations = new MathFormula[_inputs.Count];
            for (int i = 0; i < permutations.Length; i++)
            {
                var input = _inputs[i];


            }
            return permutations;
        }
    }
}
