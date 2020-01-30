using System;
using System.Collections.Generic;
using System.Globalization;
using Miniräknare.Expressions.Tokens;

namespace Miniräknare.Expressions
{
    public readonly struct ExpressionTreeEvaluator
    {
        // TODO: remove recursion

        public delegate Evaluation ResolveReferenceDelegate(ReadOnlyMemory<char> name);
        public delegate Evaluation ResolveOperatorDelegate(ReadOnlyMemory<char> name, UnionValue? left, UnionValue? right);
        public delegate Evaluation ResolveFunctionDelegate(ReadOnlyMemory<char> name, ReadOnlySpan<UnionValue> arguments);

        public ExpressionTree Tree { get; }
        public ResolveReferenceDelegate ResolveReference { get; }
        public ResolveOperatorDelegate ResolveOperator { get; }
        public ResolveFunctionDelegate ResolveFunction { get; }

        public ExpressionTreeEvaluator(
            ExpressionTree tree,
            ResolveReferenceDelegate resolveReference,
            ResolveOperatorDelegate resolveOperator,
            ResolveFunctionDelegate resolveFunction)
        {
            Tree = tree ?? throw new ArgumentNullException(nameof(tree));
            ResolveReference = resolveReference ?? throw new ArgumentNullException(nameof(resolveReference));
            ResolveOperator = resolveOperator ?? throw new ArgumentNullException(nameof(resolveOperator));
            ResolveFunction = resolveFunction ?? throw new ArgumentNullException(nameof(resolveFunction));
        }

        public Evaluation Evaluate()
        {
            if (Tree.Tokens.Count > 0 && Tree.Tokens.Count <= 3)
                return EvaluateList(Tree.Tokens);

            return Evaluation.Empty;
        }

        private Evaluation EvaluateList(List<Token> list)
        {
            if (list.Count == 1)
            {
                return EvaluateToken(list[0]);
            }
            else if (list.Count == 2)
            {
                for (int opIndex = 0; opIndex < list.Count; opIndex++)
                {
                    var token = list[opIndex];
                    if (token.Type != TokenType.Operator)
                        continue;

                    var opDefinitions = Tree.Options.OpDefinitions.Span;
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
                                return EvaluateOperator(opToken, leftToken, rightToken);
                            }
                        }
                    }
                    break;
                }
            }
            else if (list.Count == 3)
            {
                if (list[1] is ValueToken opToken)
                    return EvaluateOperator(opToken, list[0], list[2]);
            }
            return Evaluation.Undefined;
        }

        private Evaluation EvaluateToken(Token token)
        {
            if (token is ListToken listToken)
                return EvaluateList(listToken.Children);

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
                        return EvaluateReference(valueToken);

                    default:
                        return Evaluation.Undefined;
                }
            }
            else if (token is FunctionToken funcToken)
            {
                return EvaluateFunction(funcToken);
            }
            else
            {
                return Evaluation.Undefined;
            }
        }

        private Evaluation EvaluateReference(ValueToken token)
        {
            return ResolveReference.Invoke(token.Value);
        }

        private Evaluation EvaluateOperator(ValueToken opToken, Token leftToken, Token rightToken)
        {
            var opDef = Tree.Options.GetOperatorDefinition(opToken.Value.Span);
            if (leftToken == null && (
                opDef?.Sidedness == OperatorSidedness.Both ||
                opDef?.Sidedness == OperatorSidedness.Left ||
                opDef?.Sidedness == OperatorSidedness.OptionalRight))
                return new Evaluation(EvalCode.OperatorMissingLeftValue);

            if (rightToken == null && (
                opDef?.Sidedness == OperatorSidedness.Both ||
                opDef?.Sidedness == OperatorSidedness.Right ||
                opDef?.Sidedness == OperatorSidedness.OptionalLeft))
                return new Evaluation(EvalCode.OperatorMissingRightValue);

            var leftEval = leftToken != null ? EvaluateToken(leftToken) : (Evaluation?)null;
            if (leftEval.HasValue)
            {
                var leftEvalValue = leftEval.Value;
                if (leftEvalValue.Code != EvalCode.Ok)
                    return leftEvalValue;
            }

            var rightEval = rightToken != null ? EvaluateToken(rightToken) : (Evaluation?)null;
            if (rightEval.HasValue)
            {
                var rightEvalValue = rightEval.Value;
                if (rightEvalValue.Code != EvalCode.Ok)
                    return rightEvalValue;
            }

            return ResolveOperator.Invoke(opToken.Value, leftEval?.Value, rightEval?.Value);
        }

        private Evaluation EvaluateFunction(FunctionToken token)
        {
            Span<UnionValue> args = stackalloc UnionValue[token.Arguments.Count];
            for (int i = 0; i < token.Arguments.Count; i++)
            {
                var eval = EvaluateToken(token.Arguments[i]);
                if (eval.Code != EvalCode.Ok)
                    return eval;
                args[i] = eval.Value;
            }
            return ResolveFunction.Invoke(token.Name.Value, args);
        }
    }
}
