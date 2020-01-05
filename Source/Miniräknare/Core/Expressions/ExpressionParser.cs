using System;
using System.Collections.Generic;
using static Miniräknare.Expressions.ExpressionTokenizer;

namespace Miniräknare.Expressions
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
            MissingListEnd,
            ListEndWithoutStart,
            OperatorMissingLeftValue,
            OperatorMissingRightValue
        }

        public static ResultCode ParseTokens(IList<Token> tokens)
        {
            ResultCode code;
            if ((code = MakeLists(tokens)) != ResultCode.Ok ||
                (code = MakeFunctions(tokens)) != ResultCode.Ok ||
                (code = MakeOperations(tokens)) != ResultCode.Ok)
                return code;
            return code;
        }

        //private static int IndexOf(IList<Token> tokens, int offset, TokenType type)
        //{
        //    for (; offset < tokens.Count; offset++)
        //    {
        //        if (tokens[offset].Type == type)
        //            return offset;
        //    }
        //    return -1;
        //}

        private static ResultCode MakeLists(IList<Token> tokens)
        {
            var listStack = new List<List<Token>>();
            int ListDepth() => listStack.Count - 1;

            for (int i = 0; i < tokens.Count; i++)
            {
                var currentToken = tokens[i];
                var currentType = currentToken.Type;

                if (currentType == TokenType.ListStart)
                {
                    listStack.Add(new List<Token>());

                    // Remove the ListStart token.
                    tokens.RemoveAt(i--);
                }
                else if (currentType == TokenType.ListEnd)
                {
                    if (listStack.Count == 0)
                        return ResultCode.ListEndWithoutStart;

                    var listToken = new ListToken(listStack[ListDepth()]);
                    listStack.RemoveAt(ListDepth());

                    if (ListDepth() > -1)
                    {
                        listStack[ListDepth()].Add(listToken);

                        // Remove the ListEnd token.
                        tokens.RemoveAt(i);
                    }
                    else
                    {
                        // This replaces the ListEnd token.
                        tokens[i] = listToken;
                    }
                    i--;
                }
                else
                {
                    if (ListDepth() > -1)
                    {
                        listStack[ListDepth()].Add(currentToken);
                        tokens.RemoveAt(i--);
                    }
                    else
                    {
                        // We don't do anything with tokens outside lists.
                    }
                }
            }
            
            if (ListDepth() > -1)
                return ResultCode.MissingListEnd;

            return ResultCode.Ok;
        }

        private static ResultCode MakeFunctions(IList<Token> tokens)
        {

            return ResultCode.Ok;
        }

        private static ResultCode MakeOperations(IList<Token> tokens)
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
