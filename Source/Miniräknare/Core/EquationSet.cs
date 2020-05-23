using System;
using System.Collections.Generic;
using Miniräknare.Expressions;
using Miniräknare.Expressions.Tokens;

namespace Miniräknare
{
    public class EquationSet : IExpressionTree
    {
        private ExpressionTree _tree;
        private List<ValueToken> _inputs;

        public ExpressionSanitizer.SanitizeResult SanitizeResult { get; }
        public ExpressionParser.ResultCode ParseCode { get; }

        public bool IsValid =>
            SanitizeResult.Code == ExpressionSanitizer.ResultCode.Ok &&
            ParseCode == ExpressionParser.ResultCode.Ok;

        public ReadOnlyMemory<(ReadOnlyString Target, ExpressionTree Tree)> Permutations { get; }

        #region IExpressionTree

        public ExpressionOptions Options => _tree.Options;
        public IReadOnlyList<Token> Tokens => _tree.Tokens;

        #endregion

        public EquationSet(ExpressionTree tree)
        {
            _tree = tree?.Clone() ?? throw new ArgumentNullException(nameof(tree));

            SanitizeResult = ExpressionSanitizer.Sanitize(_tree);
            if (SanitizeResult.Code != ExpressionSanitizer.ResultCode.Ok)
                throw new Exception(SanitizeResult.Code.ToString());

            ParseCode = ExpressionParser.Parse(_tree);
            if (ParseCode != ExpressionParser.ResultCode.Ok)
                throw new Exception(ParseCode.ToString());

            _inputs = GatherInputs(_tree);
            Permutations = CreatePermutations();
        }

        public EquationSet(ExpressionOptions options, ReadOnlyMemory<char> equation)
            : this(CreateTree(options, equation))
        {
        }

        public EquationSet(ExpressionOptions options, EquationSetData data)
            : this(options, data.Equation.AsMemory())
        {
        }

        private static ExpressionTree CreateTree(
            ExpressionOptions options, ReadOnlyMemory<char> equation)
        {
            var tree = new ExpressionTree(options);
            ExpressionTokenizer.Tokenize(equation, tree.Tokens);
            return tree;
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

        private (ReadOnlyString Target, ExpressionTree Tree)[] CreatePermutations()
        {
            // TODO: put this into a Solve() func

            var options = _tree.Options;
            var permutations = new (ReadOnlyString Target, ExpressionTree Tree)[_inputs.Count];

            for (int i = 0; i < permutations.Length; i++)
            {
                var target = _inputs[i];

                var path = CreateTokenTreePath(target);
                path.Reverse();

                var result = new ListToken();
                result.Add(new ValueToken(TokenType.Name, "§input".AsMemory()));

                for (int j = 0; j < path.Count; j++)
                {
                    var pathToken = path[j];

                    if (pathToken is ListToken pathList)
                    {
                        if (pathList.Count == 3)
                        {
                            var opDef = options.GetOperatorDefinition(((ValueToken)pathList[1]).Value);
                            var inverseOpDef = options.GetOperatorInverse(opDef.Type);
                            if (inverseOpDef == null)
                                throw new NotSupportedException(
                                    "Missing inverse operator for \"" + opDef.Names[0] + "\".");

                            var inverseOpDefName = inverseOpDef.Definition.Names[0];
                            var inverseOp = new ValueToken(TokenType.Operator, inverseOpDefName);
                            var inverseValue = pathList[2];

                            if (inverseValue.IsOrContains(target))
                                inverseValue = pathList[0];

                            result.Children.Add(inverseOp);
                            result.Children.Add(inverseValue);

                            var newResult = new ListToken();
                            newResult.Children.Add(result);
                            result = newResult;
                        }
                        else if (pathList.Count == 2)
                        {
                            throw new NotImplementedException();
                        }
                        else if (pathList.Count == 1)
                        {
                            continue;
                        }
                        else
                        {
                            throw new NotSupportedException();
                        }
                    }
                    else if (pathToken is FunctionToken pathFunc)
                    {
                        var funcName = pathFunc.Name.Value.Span;
                        var inverseFuncName = FindInverseFunction(funcName);
                        if (inverseFuncName.IsEmpty)
                            throw new NotSupportedException(
                                "Missing inverse function for \"" + funcName.ToString() + "\".");

                        var name = new ValueToken(TokenType.Name, inverseFuncName);
                        var func = new FunctionToken(name, new List<Token>() { result });

                        var newResult = new ListToken();
                        newResult.Children.Add(func);
                        result = newResult;
                    }
                    else if (pathToken == target)
                    {
                        break;
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }

                var resultTree = new ExpressionTree(options, result.Children);
                permutations[i] = (target.Value, resultTree);
            }

            return permutations;
        }

        private ReadOnlyString FindInverseFunction(ReadOnlySpan<char> name)
        {
            var options = _tree.Options;

            if (name.SequenceEqual("sin"))
            {
                return "arcsin".AsMemory();
            }

            return ReadOnlyString.Empty;
        }

        private List<Token> CreateTokenTreePath(Token target)
        {
            var path = new List<Token>();
            path.Add(target);

            Token parent = target;
            while ((parent = parent.Parent) != null)
            {
                path.Add(parent);
            }

            path.Add(new ListToken(_tree.Tokens));

            return path;
        }
    }
}
