using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Newtonsoft.Json.Linq;
using WpfCalculator.Expressions.Tokens;

namespace WpfCalculator.Expressions
{
    public class ExpressionTreeEvaluator
    {
        // TODO: remove recursion

        public delegate Evaluation ResolveReferenceDelegate(string name);
        public delegate Evaluation ExecuteOperatorDelegate(string name, JToken? left, JToken? right);
        public delegate Evaluation ExecuteFunctionDelegate(string name, params JToken?[] arguments);

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
                string value = valueToken.Value;
                switch (valueToken.Type)
                {
                    case TokenType.DecimalDigit:
                        return new Evaluation(CharUnicodeInfo.GetNumericValue(value[0]));

                    case TokenType.DecimalNumber:
                        if (value.Length == 1)
                            return new Evaluation(CharUnicodeInfo.GetNumericValue(value[0]));
                        else
                        {
                            var parsed = double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
                            return new Evaluation(parsed);
                        }

                    case TokenType.Name:
                        return ResolveReference.Invoke(valueToken.Value);

                    default:
                        return new Evaluation(EErrorCode.SyntaxError);
                }
            }
            else if (token is FunctionToken funcToken)
            {
                return EvaluateFunction(options, funcToken);
            }
            return new Evaluation(EErrorCode.Undefined);
        }

        public Evaluation EvaluateTree(ExpressionTree tree)
        {
            return EvaluateList(tree.Options, tree.Tokens);
        }

        public Evaluation EvaluateList(ExpressionOptions options, List<Token> tokens)
        {
            if (tokens.Count == 0)
                return new Evaluation(EErrorCode.Empty);

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
            [MaybeNullWhen(false)] out ValueToken opToken, 
            out Token? leftToken, 
            out Token? rightToken)
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
                            if (!name.Span.SequenceEqual(opToken.Value))
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

            var evaluations = new object?[valueCount];
            int evalIndex = 0;
            for (int i = 0; i < tokens.Count; i++)
            {
                if (tokens[i].Type == TokenType.ListSeparator)
                    continue;

                var eval = Evaluate(options, tokens[i]);
                if (eval.Error != null)
                    return eval;

                evaluations[evalIndex++] = eval.Result;
            }
            return new Evaluation(evaluations);
        }

        public static EError? ValidateOperatorTokens(
            ExpressionOptions options, ValueToken op, Token? left, Token? right)
        {
            var opDef = options.GetOperatorDefinition(op.Value);
            if (opDef != null)
            {
                if (left == null && (
                    opDef.Associativity == OperatorSidedness.Both ||
                    opDef.Associativity.HasFlag(OperatorSidedness.Left)))
                    return EErrorCode.OperatorMissingLeftValue;

                if (right == null && (
                    opDef.Associativity == OperatorSidedness.Both ||
                    opDef.Associativity.HasFlag(OperatorSidedness.Right)))
                    return EErrorCode.OperatorMissingRightValue;
            }
            return null;
        }

        public Evaluation EvaluateOperator(
            ExpressionOptions options, ValueToken op, Token? left, Token? right)
        {
            var validationError = ValidateOperatorTokens(options, op, left, right);
            if (validationError != null)
                return validationError;

            var leftEval = left != null ? Evaluate(options, left) : null;
            if (leftEval?.Error != null)
                return leftEval;

            var rightEval = right != null ? Evaluate(options, right) : null;
            if (rightEval?.Error != null)
                return rightEval;

            return ExecuteOperator.Invoke(op.Value, leftEval?.Result, rightEval?.Result);
        }

        public Evaluation EvaluateFunction(ExpressionOptions options, FunctionToken function)
        {
            var arguments = new JToken?[function.ArgumentCount];
            int valueIndex = 0;
            for (int i = 0; i < function.Children.Count; i++)
            {
                var arg = function.Children[i];
                if (arg.Type == TokenType.ListSeparator)
                    continue;

                var eval = Evaluate(options, arg);
                if (eval.Error != null)
                    return eval;

                arguments[valueIndex] = eval.Result;
                valueIndex++;
            }
            return ExecuteFunction.Invoke(function.Name.Value, arguments);
        }
    }
}
