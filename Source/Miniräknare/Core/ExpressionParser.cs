using System;
using System.Collections.Generic;
using static Miniräknare.ExpressionTokenizer;

namespace Miniräknare
{
    public partial class ExpressionParser
    {
        public static int TokenIndexToCharIndex(IReadOnlyList<Token> tokens, int tokenIndex)
        {
            int charIndex = 0;
            for (int i = 0; i < tokenIndex; i++)
                charIndex += tokens[i].Value.Length;
            return charIndex;
        }

        public enum ResultCode
        {
            Ok = 0,
            OperatorMissingLeftValue,
            OperatorMissingRightValue
        }

        public static ResultCode ParseTokens(IReadOnlyList<Token> tokens)
        {
            for (int i = 0; i < tokens.Count; i++)
            {
                var currentToken = tokens[i];
                if (currentToken.Type == TokenType.Operator)
                {
                    // '+' and '-' don't error when missing left value
                    if (!currentToken.ValueEqualTo('+') &&
                        !currentToken.ValueEqualTo('-'))
                    {
                        if (i - 1 < 0)
                            return ResultCode.OperatorMissingLeftValue;
                    }

                    if (i + 1 >= tokens.Count)
                        return ResultCode.OperatorMissingRightValue;

                    var leftToken = tokens[i - 1];
                    var rightToken = tokens[i + 1];

                    Console.WriteLine(leftToken + currentToken.Value.ToString() + rightToken);
                }
            }
            return ResultCode.Ok;
        }
    }
}
