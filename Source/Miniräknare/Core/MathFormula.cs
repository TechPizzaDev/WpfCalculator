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

        public ExpressionSanitizer.SanitizeResult SanitizeResult { get; }
        public ExpressionParser.ResultCode ParseCode { get; }

        public bool IsValid =>
            SanitizeResult.Code == ExpressionSanitizer.ResultCode.Ok &&
            ParseCode == ExpressionParser.ResultCode.Ok;

        public ReadOnlyMemory<MathFormula> Permutations { get; }

        #region IExpressionTree

        public ExpressionOptions ExpressionOptions => _tree.ExpressionOptions;
        public IReadOnlyList<Token> Tokens => _tree.Tokens.Children;

        #endregion

        public MathFormula(ExpressionOptions options, ReadOnlyMemory<char> formula)
        {
            _tree = new ExpressionTree(options);
            ExpressionTokenizer.Tokenize(formula, _tree.Tokens);

            SanitizeResult = ExpressionSanitizer.Sanitize(_tree);
            if (SanitizeResult.Code != ExpressionSanitizer.ResultCode.Ok)
                return;

            ParseCode = ExpressionParser.Parse(_tree);
            if (ParseCode != ExpressionParser.ResultCode.Ok)
                return;

            _inputs = GatherInputs(_tree);
            Permutations = CreatePermutations();
        }

        public MathFormula(ExpressionOptions options, MathFormulaData data)
            : this(options, data.Formula.AsMemory())
        {
        }

        private static List<ValueToken> GatherInputs(ExpressionTree tree)
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
            // TODO: put this into a Solve() func

            var permutations = new MathFormula[_inputs.Count];
            for (int i = 0; i < permutations.Length; i++)
            {
                var target = _inputs[i];

                var path = CreateTokenTreePath(target);

                throw new NotImplementedException();
            }
            return permutations;
        }

        private List<Token> CreateTokenTreePath(Token target)
        {
            var tree = new List<Token>();
            tree.Add(target);

            Token parent = target;
            while((parent = parent.Parent) != null)
            {
                tree.Add(parent);
            }

            return tree;
        }
    }
}
