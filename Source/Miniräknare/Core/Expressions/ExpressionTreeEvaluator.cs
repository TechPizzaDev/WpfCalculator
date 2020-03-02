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
            ReadOnlyMemory<char> name, UnionValueCollection? left, UnionValueCollection? right);

        public delegate Evaluation ExecuteFunctionDelegate(
            ReadOnlyMemory<char> name, ReadOnlySpan<UnionValueCollection> arguments);

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
            return Evaluate(tree.ExpressionOptions, tree.Tokens);
        }

        public Evaluation Evaluate(ExpressionOptions options, List<Token> tokens)
        {
            if (tokens.Count == 0)
            {
                return Evaluation.Empty;
            }
            else if (tokens.Count == 1)
            {
                return EvaluateToken(options, tokens[0]);
            }
            else if (tokens.Count == 2)
            {
                for (int opIndex = 0; opIndex < tokens.Count; opIndex++)
                {
                    var token = tokens[opIndex];
                    if (token.Type != TokenType.Operator)
                        continue;

                    var opDefinitions = options.OpDefinitions.Span;
                    for (int j = 0; j < opDefinitions.Length; j++)
                    {
                        var opDef = opDefinitions[j];
                        if (opDef.Associativity == OperatorAssociativity.Both)
                            continue;

                        var opToken = (ValueToken)token;
                        for (int k = 0; k < opDef.Names.Length; k++)
                        {
                            var name = opDef.Names[k];
                            if (name.Span.SequenceEqual(opToken.Value.Span))
                            {
                                var leftToken = opIndex == 0 ? null : tokens[0];
                                var rightToken = opIndex == 1 ? null : tokens[1];
                                return EvaluateOperator(options, opToken, leftToken, rightToken);
                            }
                        }
                    }
                    break;
                }
            }
            else if (tokens.Count == 3)
            {
                if (tokens[1] is ValueToken opToken)
                    return EvaluateOperator(options, opToken, tokens[0], tokens[2]);
            }
            return Evaluation.Undefined;
        }

        public Evaluation EvaluateToken(ExpressionOptions options, Token token)
        {
            if (token is ListToken listToken)
                return Evaluate(options, listToken.Children);

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
            return Evaluation.Undefined;
        }

        public Evaluation EvaluateOperator(
            ExpressionOptions options, ValueToken op, Token left, Token right)
        {
            var opDef = options.GetOperatorDefinition(op.Value);
            if (left == null && (
                opDef?.Associativity == OperatorAssociativity.Both ||
                opDef?.Associativity == OperatorAssociativity.Left))
                return new Evaluation(EvalCode.OperatorMissingLeftValue);

            if (right == null && (
                opDef?.Associativity == OperatorAssociativity.Both ||
                opDef?.Associativity == OperatorAssociativity.Right))
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

            return ExecuteOperator.Invoke(op.Value, leftEval?.Values, rightEval?.Values);
        }

        public Evaluation EvaluateFunction(ExpressionOptions options, FunctionToken function)
        {
            var argValues = new UnionValueCollection[function.ArgumentCount];
            int valueIndex = 0;
            for (int i = 0; i < function.ArgumentList.Count; i++)
            {
                if (function.ArgumentList[i].Type == TokenType.ListSeparator)
                    continue;

                var eval = EvaluateToken(options, function.ArgumentList[i]);
                if (eval.Code != EvalCode.Ok)
                    return eval;

                argValues[valueIndex] = eval.Values;
                valueIndex++;
            }
            return ExecuteFunction.Invoke(function.Name.Value, argValues);
        }
    }
}
