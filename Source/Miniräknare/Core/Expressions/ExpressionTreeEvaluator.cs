using System;
using System.Collections.Generic;
using System.Globalization;
using Miniräknare.Expressions.Tokens;

namespace Miniräknare.Expressions
{
    public class ExpressionTreeEvaluator
    {
        // TODO: remove recursion

        public delegate Evaluation ResolveReferenceDelegate(
            ReadOnlyMemory<char> name);

        public delegate Evaluation ExecuteOperatorDelegate(
            ReadOnlyMemory<char> name, UnionValue? left, UnionValue? right);

        public delegate Evaluation ExecuteFunctionDelegate(
            ReadOnlyMemory<char> name, ReadOnlySpan<UnionValue> arguments);

        public ResolveReferenceDelegate ResolveReference { get; }
        public ExecuteOperatorDelegate ExecuteOperator { get; }
        public ExecuteFunctionDelegate ExecuteFunction { get; }

        public ExpressionTreeEvaluator(
            ResolveReferenceDelegate resolveReference,
            ExecuteOperatorDelegate resolveOperator,
            ExecuteFunctionDelegate resolveFunction)
        {
            ResolveReference = resolveReference ?? throw new ArgumentNullException(nameof(resolveReference));
            ExecuteOperator = resolveOperator ?? throw new ArgumentNullException(nameof(resolveOperator));
            ExecuteFunction = resolveFunction ?? throw new ArgumentNullException(nameof(resolveFunction));
        }

        public Evaluation Evaluate(ExpressionTree tree)
        {
            if (tree.Tokens.Count > 0 && tree.Tokens.Count <= 3)
                return EvaluateList(tree.Options, tree.Tokens);
            return Evaluation.Empty;
        }

        private Evaluation EvaluateList(ExpressionOptions options, List<Token> list)
        {
            if (list.Count == 1)
            {
                return EvaluateToken(options, list[0]);
            }
            else if (list.Count == 2)
            {
                for (int opIndex = 0; opIndex < list.Count; opIndex++)
                {
                    var token = list[opIndex];
                    if (token.Type != TokenType.Operator)
                        continue;

                    var opDefinitions = options.OpDefinitions.Span;
                    for (int j = 0; j < opDefinitions.Length; j++)
                    {
                        var opDef = opDefinitions[j];
                        if (opDef.Sidedness == OperatorSidedness.Both)
                            continue;

                        var opToken = (ValueToken)token;
                        for (int k = 0; k < opDef.Names.Length; k++)
                        {
                            var name = opDef.Names[k];
                            if (name.Span.SequenceEqual(opToken.Value.Span))
                            {
                                var leftToken = opIndex == 0 ? null : list[0];
                                var rightToken = opIndex == 1 ? null : list[1];
                                return EvaluateOperator(options, opToken, leftToken, rightToken);
                            }
                        }
                    }
                    break;
                }
            }
            else if (list.Count == 3)
            {
                if (list[1] is ValueToken opToken)
                    return EvaluateOperator(options, opToken, list[0], list[2]);
            }
            return Evaluation.Undefined;
        }

        private Evaluation EvaluateToken(ExpressionOptions options, Token token)
        {
            if (token is ListToken listToken)
                return EvaluateList(options, listToken.Children);

            if (token is ValueToken valueToken)
            {
                var value = valueToken.Value.Span;
                switch (valueToken.Type)
                {
                    case TokenType.DecimalDigit:
                        return CharUnicodeInfo.GetNumericValue(value[0]);

                    case TokenType.DecimalNumber:
                        if (value.Length == 1)
                            return CharUnicodeInfo.GetNumericValue(value[0]);
                        else
                            return double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);

                    case TokenType.Name:
                        return ResolveReference.Invoke(valueToken.Value);

                    default:
                        return Evaluation.Undefined;
                }
            }
            else if (token is FunctionToken funcToken)
            {
                return EvaluateFunction(options, funcToken);
            }
            else
            {
                return Evaluation.Undefined;
            }
        }

        private Evaluation EvaluateOperator(
            ExpressionOptions options, ValueToken op, Token left, Token right)
        {
            var opDef = options.GetOperatorDefinition(op.Value.Span);
            if (left == null && (
                opDef?.Sidedness == OperatorSidedness.Both ||
                opDef?.Sidedness == OperatorSidedness.Left ||
                opDef?.Sidedness == OperatorSidedness.OptionalRight))
                return new Evaluation(EvalCode.OperatorMissingLeftValue);

            if (right == null && (
                opDef?.Sidedness == OperatorSidedness.Both ||
                opDef?.Sidedness == OperatorSidedness.Right ||
                opDef?.Sidedness == OperatorSidedness.OptionalLeft))
                return new Evaluation(EvalCode.OperatorMissingRightValue);

            var leftEval = left != null ? EvaluateToken(options, left) : (Evaluation?)null;
            if (leftEval.HasValue)
            {
                var leftEvalValue = leftEval.Value;
                if (leftEvalValue.Code != EvalCode.Ok)
                    return leftEvalValue;
            }

            var rightEval = right != null ? EvaluateToken(options, right) : (Evaluation?)null;
            if (rightEval.HasValue)
            {
                var rightEvalValue = rightEval.Value;
                if (rightEvalValue.Code != EvalCode.Ok)
                    return rightEvalValue;
            }

            return ExecuteOperator.Invoke(op.Value, leftEval?.Value, rightEval?.Value);
        }

        private Evaluation EvaluateFunction(ExpressionOptions options, FunctionToken token)
        {
            Span<UnionValue> args = stackalloc UnionValue[token.Arguments.Count];
            for (int i = 0; i < token.Arguments.Count; i++)
            {
                var eval = EvaluateToken(options, token.Arguments[i]);
                if (eval.Code != EvalCode.Ok)
                    return eval;
                args[i] = eval.Value;
            }
            return ExecuteFunction.Invoke(token.Name.Value, args);
        }
    }
}
