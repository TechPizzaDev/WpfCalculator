using System;
using System.Collections.Generic;
using Miniräknare.Expressions.Tokens;

namespace Miniräknare.Expressions
{
    public static class ExpressionReducer
    {
        public enum ResultCode
        {
            Ok
        }

        public static ResultCode Reduce(List<Token> tokens, ExpressionOptions options)
        {
            // TODO: the reducer can currently break expressions pretty hard, so fix it
            throw new NotImplementedException();

            if (tokens == null) throw new ArgumentNullException(nameof(tokens));
            if (options == null) throw new ArgumentNullException(nameof(options));

            ResultCode code;
            if ((code = ReduceLists(tokens, options)) != ResultCode.Ok)
                return code;
            return code;
        }

        public static ResultCode Reduce(ExpressionTree tree)
        {
            return Reduce(tree.Tokens, tree.ExpressionOptions);
        }

        private static ResultCode ReduceLists(List<Token> tokens, ExpressionOptions options)
        {
            var listStack = new Stack<List<Token>>();
            listStack.Push(tokens);

            while (listStack.Count > 0)
            {
                var currentTokens = listStack.Pop();
                for (int i = 0; i < currentTokens.Count; i++)
                {
                    var token = currentTokens[i];
                    if (token.Type == TokenType.List)
                    {
                        var listToken = (ListToken)token;
                        if (listToken.Count == 1)
                        {
                            var listChild = listToken[0];
                            currentTokens[i] = listChild;
                            i--;
                        }
                        listStack.Push(listToken.Children);
                    }
                }
            }
            return ResultCode.Ok;
        }
    }
}
