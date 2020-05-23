using System;
using System.Collections.Generic;
using System.Globalization;
using WpfCalculator.Expressions.Tokens;

namespace WpfCalculator.Expressions
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

        public Evaluation Evaluate(ExpressionOptions options, Token token)
        {
            if (token is ListToken listToken)
                return EvaluateList(options, listToken.Children);

            if (token is ValueToken valueToken)
            {
                var value = valueToken.Value.Span;
                switch (valueToken.Type)
                {
                    case TokenType.DecimalDigit:
                        return new Evaluation(new UnionValue(CharUnicodeInfo.GetNumericValue(value[0])));

                    case TokenType.DecimalNumber:
                        if (value.Length == 1)
                            return new Evaluation(new UnionValue(CharUnicodeInfo.GetNumericValue(value[0])));
                        else
                            return new Evaluation(new UnionValue(
                                double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture)));

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

        public Evaluation EvaluateTree(ExpressionTree tree)
        {
            return EvaluateList(tree.Options, tree.Tokens);
        }

        public Evaluation EvaluateList(ExpressionOptions options, List<Token> tokens)
        {
            if (tokens.Count == 0)
                return Evaluation.Undefined;

            if (tokens.Count == 1)
                return Evaluate(options, tokens[0]);

            if (ValidateOperatorList(
                options, tokens,
                out var opToken, out var leftToken, out var rightToken))
                return EvaluateOperator(options, opToken, leftToken, rightToken);

            return ListToCollection(options, tokens);
        }

        private bool ValidateOperatorList(
            ExpressionOptions options, List<Token> tokens,
            out ValueToken opToken, out Token leftToken, out Token rightToken)
        {
            if (tokens.Count == 2)
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
                        if (opDef.Associativity == OperatorSidedness.Both)
                            continue;

                        opToken = (ValueToken)token;
                        for (int k = 0; k < opDef.Names.Length; k++)
                        {
                            var name = opDef.Names[k];
                            if (!name.Span.SequenceEqual(opToken.Value.Span))
                                continue;

                            leftToken = opIndex == 0 ? null : tokens[0];
                            rightToken = opIndex == 1 ? null : tokens[1];
                            return true;
                        }
                    }
                    break;
                }
            }

            if (tokens.Count == 3 &&
                tokens[1] is ValueToken middleToken &&
                middleToken.Type == TokenType.Operator)
            {
                opToken = middleToken;
                leftToken = tokens[0];
                rightToken = tokens[2];
                return true;
            }

            opToken = null;
            leftToken = null;
            rightToken = null;
            return false;
        }

        private Evaluation ListToCollection(ExpressionOptions options, List<Token> tokens)
        {
            int valueCount = 0;
            for (int i = 0; i < tokens.Count; i++)
                if (tokens[i].Type != TokenType.ListSeparator)
                    valueCount++;

            var evaluations = new UnionValueCollection[valueCount];
            int evalIndex = 0;
            for (int i = 0; i < tokens.Count; i++)
            {
                if (tokens[i].Type == TokenType.ListSeparator)
                    continue;

                var eval = Evaluate(options, tokens[i]);
                if (eval.Code != EvalCode.Ok)
                    return eval;

                evaluations[evalIndex++] = eval.Values;
            }
            return new Evaluation(new UnionValueCollection(evaluations));
        }

        public static EvalCode ValidateOperatorTokens(
            ExpressionOptions options, ValueToken op, Token left, Token right)
        {
            var opDef = options.GetOperatorDefinition(op.Value);
            if (opDef != null)
            {
                if (left == null && (
                    opDef.Associativity == OperatorSidedness.Both ||
                    opDef.Associativity.HasFlag(OperatorSidedness.Left)))
                    return EvalCode.OperatorMissingLeftValue;

                if (right == null && (
                    opDef.Associativity == OperatorSidedness.Both ||
                    opDef.Associativity.HasFlag(OperatorSidedness.Right)))
                    return EvalCode.OperatorMissingRightValue;
            }
            return EvalCode.Ok;
        }

        public Evaluation EvaluateOperator(
            ExpressionOptions options, ValueToken op, Token left, Token right)
        {
            var setCode = ValidateOperatorTokens(options, op, left, right);
            if (setCode != EvalCode.Ok)
                return new Evaluation(setCode);

            var leftEval = left != null ? Evaluate(options, left) : (Evaluation?)null;
            if (leftEval.HasValue)
            {
                var leftEvalValue = leftEval.Value;
                if (leftEvalValue.Code != EvalCode.Ok)
                    return leftEvalValue;
            }

            var rightEval = right != null ? Evaluate(options, right) : (Evaluation?)null;
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
            for (int i = 0; i < function.Children.Count; i++)
            {
                var arg = function.Children[i];
                if (arg.Type == TokenType.ListSeparator)
                    continue;

                var eval = Evaluate(options, arg);
                if (eval.Code != EvalCode.Ok)
                    return eval;

                argValues[valueIndex] = eval.Values;
                valueIndex++;
            }
            return ExecuteFunction.Invoke(function.Name.Value, argValues);
        }
    }
}
