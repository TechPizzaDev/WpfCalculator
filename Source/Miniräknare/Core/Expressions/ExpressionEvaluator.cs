using System;
using System.Collections.Generic;
using System.Globalization;
using Miniräknare.Expressions.Tokens;

namespace Miniräknare.Expressions
{
    public class ExpressionEvaluator
    {
        public delegate object ResolveReferenceDelegate(ReadOnlyMemory<char> name);
        public delegate object ResolveOperatorDelegate(ReadOnlyMemory<char> name, object left, object right);
        public delegate object ResolveFunctionDelegate(ReadOnlyMemory<char> name, IEnumerable<object> arguments);

        public ResolveReferenceDelegate ResolveReference { get; }
        public ResolveOperatorDelegate ResolveOperator { get; }
        public ResolveFunctionDelegate ResolveFunction { get; }

        public ExpressionEvaluator(
            ResolveReferenceDelegate resolveReference, 
            ResolveOperatorDelegate resolveOperator, 
            ResolveFunctionDelegate resolveFunction)
        {
            ResolveReference = resolveReference ?? throw new ArgumentNullException(nameof(resolveReference));
            ResolveOperator = resolveOperator ?? throw new ArgumentNullException(nameof(resolveOperator));
            ResolveFunction = resolveFunction ?? throw new ArgumentNullException(nameof(resolveFunction));
        }

        public object Evaluate(ExpressionTree tree)
        {
            if (tree.Tokens.Count == 1)
                return EvaluateToken(tree.Tokens[0]);
            return null;
        }

        private object EvaluateList(ListToken listToken)
        {
            if (listToken.Count == 1)
                return EvaluateToken(listToken[0]);

            if (listToken.Count == 2 &&
                listToken[0] is ValueToken valueToken &&
                valueToken.Type == TokenType.Operator &&
                valueToken.ValueEqualTo('-'))
                return EvaluateNegativeList(listToken[1]);

            if (listToken.Count == 3 &&
                listToken[1] is ValueToken opToken)
                return EvaluateOperatorList(opToken, listToken[0], listToken[2]);

            return null;
        }

        private object EvaluateToken(Token token)
        {
            if (token is ListToken listToken)
                return EvaluateList(listToken);

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
                        return null;
                }
            }
            else if (token is FunctionToken funcToken)
            {
                return EvaluateFunction(funcToken);
            }
            else
            {
                return null;
            }
        }

        private object EvaluateNegativeList(Token token)
        {
            throw new NotImplementedException();
        }

        private object EvaluateReference(ValueToken token)
        {
            return ResolveReference.Invoke(token.Value);
        }

        private object EvaluateFunction(FunctionToken token)
        {
            return ResolveFunction.Invoke(token.Name.Value, token.Parameters);
        }

        private object EvaluateOperatorList(ValueToken opToken, Token leftToken, Token rightToken)
        {
            var leftValue = EvaluateToken(leftToken);
            var rightValue = EvaluateToken(rightToken);
            return ResolveOperator.Invoke(opToken.Value, leftValue, rightValue);
        }
    }
}
