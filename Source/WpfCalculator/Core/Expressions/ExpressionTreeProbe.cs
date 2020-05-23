using System.Collections.Generic;
using WpfCalculator.Expressions.Tokens;

namespace WpfCalculator.Expressions
{
    public struct ExpressionTreeProbe
    {
        public delegate void ProbeReferenceDelegate(ValueToken reference);
        public delegate void ProbeOperatorDelegate(ValueToken op, Token left, Token right);
        public delegate void ProbeFunctionDelegate(FunctionToken function);

        public event ProbeReferenceDelegate ProbeReference;
        public event ProbeOperatorDelegate ProbeOperator;
        public event ProbeFunctionDelegate ProbeFunction;

        public void Probe(ExpressionTree tree)
        {
            Probe(tree.Tokens);
        }

        public void Probe(List<Token> tokens)
        {
            var listStack = new Stack<List<Token>>();
            listStack.Push(tokens);

            while (listStack.Count > 0)
            {
                var currentTokens = listStack.Pop();
                for (int i = 0; i < currentTokens.Count; i++)
                {
                    var token = currentTokens[i];
                    if (token is CollectionToken collection)
                        listStack.Push(collection.Children);

                    switch (token.Type)
                    {
                        case TokenType.Function:
                            ProbeFunction?.Invoke((FunctionToken)token);
                            break;

                        case TokenType.Name:
                            ProbeReference?.Invoke((ValueToken)token);
                            break;

                        case TokenType.Operator:
                        {
                            var left = i > 0 ? currentTokens[i - 1] : default;
                            var right = i + 1 < currentTokens.Count ? currentTokens[i + 1] : default;
                            ProbeOperator?.Invoke((ValueToken)token, left, right);
                            break;
                        }
                    }
                }
            }
        }
    }
}
