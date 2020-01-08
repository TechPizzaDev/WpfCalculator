using System;
using System.Collections.Generic;
using System.Globalization;
using Miniräknare.Expressions.Tokens;

namespace Miniräknare.Expressions
{
    public class ExpressionEvaluator
    {
        public delegate UnionValue ResolveReferenceDelegate(ReadOnlyMemory<char> name);
        public delegate UnionValue ResolveOperatorDelegate(ReadOnlyMemory<char> name, UnionValue left, UnionValue right);
        public delegate UnionValue ResolveFunctionDelegate(ReadOnlyMemory<char> name, ReadOnlySpan<UnionValue> arguments);

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

        public UnionValue Evaluate(ExpressionTree tree)
        {
            if (tree.Tokens.Count > 0 && tree.Tokens.Count <= 3)
                return EvaluateList(tree.Tokens);

            return UnionValue.Null;
        }

        private UnionValue EvaluateList(List<Token> list)
        {
            if (list.Count == 1)
                return EvaluateToken(list[0]);

            if (list.Count == 2 &&
                list[0] is ValueToken valueToken &&
                valueToken.Type == TokenType.Operator &&
                valueToken.ValueEqualTo('-'))
                return EvaluateOperator(valueToken, null, list[1]);

            if (list.Count == 3 &&
                list[1] is ValueToken opToken)
                return EvaluateOperator(opToken, list[0], list[2]);

            return UnionValue.Null;
        }

        private UnionValue EvaluateToken(Token token)
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
                        return UnionValue.Null;
                }
            }
            else if (token is FunctionToken funcToken)
            {
                return EvaluateFunction(funcToken);
            }
            else
            {
                return UnionValue.Null;
            }
        }

        private UnionValue EvaluateReference(ValueToken token)
        {
            return ResolveReference.Invoke(token.Value);
        }

        private UnionValue EvaluateFunction(FunctionToken token)
        {
            Span<UnionValue> args = stackalloc UnionValue[token.Arguments.Count];
            for (int i = 0; i < token.Arguments.Count; i++)
                args[i] = EvaluateToken(token.Arguments[i]);
            
            return ResolveFunction.Invoke(token.Name.Value, args);
        }

        private UnionValue EvaluateOperator(ValueToken opToken, Token leftToken, Token rightToken)
        {
            var leftValue = EvaluateToken(leftToken);
            var rightValue = EvaluateToken(rightToken);
            return ResolveOperator.Invoke(opToken.Value, leftValue, rightValue);
        }
    }
}
