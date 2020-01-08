using System;
using System.Collections.Generic;
using Miniräknare.Expressions.Tokens;

namespace Miniräknare.Expressions
{
    public partial class ExpressionParser
    {
        public enum ResultCode
        {
            Ok = 0,
            MissingListEnd,
            ListEndWithoutStart,
            OperatorMissingLeftValue,
            OperatorMissingRightValue,
            InvalidTokenBeforeList
        }

        public static ResultCode ParseTokens(List<Token> tokens)
        {
            ResultCode code;
            if ((code = MakeLists(tokens)) != ResultCode.Ok ||
                (code = MakeFunctions(tokens)) != ResultCode.Ok ||
                (code = MakeOperations(tokens)) != ResultCode.Ok)
                return code;
            return code;
        }

        #region MakeLists

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

        #endregion

        #region MakeFunctions

        private static ResultCode MakeFunctions(IList<Token> tokens)
        {
            // Loop from the end so we can call "MakeFunctions" recursively.
            for (int i = tokens.Count; i-- > 0;)
            {
                var currentToken = tokens[i];
                if (currentToken.Type == TokenType.List)
                {
                    var listToken = (ListToken)currentToken;
                    ResultCode code;
                    if ((code = MakeFunctions(listToken.Children)) != ResultCode.Ok)
                        return code;

                    if (i - 1 < 0)
                        continue; // We reached the list's beginning.

                    var leftToken = tokens[i - 1];
                    if (leftToken.Type != TokenType.Name)
                    {
                        if (leftToken.Type == TokenType.Operator)
                            continue;
                        return ResultCode.InvalidTokenBeforeList;
                    }

                    var nameToken = (ValueToken)leftToken;
                    var funcToken = new FunctionToken(nameToken, listToken);

                    tokens[i - 1] = funcToken; // replace left token
                    tokens.RemoveAt(i); // remove current token
                }
            }
            return ResultCode.Ok;
        }

        #endregion

        #region MakeOperations

        public class OperatorDefinition
        {
            public ReadOnlyMemory<char> Name { get; }
            public bool RequiresBothSides { get; }
            public int Priority { get; }

            public OperatorDefinition(ReadOnlyMemory<char> name, bool requiresBothSides, int priority)
            {
                if (name.IsEmpty)
                    throw new ArgumentException(nameof(name));

                Name = name;
                RequiresBothSides = requiresBothSides;
                Priority = priority;
            }
        }

        private static ResultCode MakeOperations(List<Token> tokens)
        {
            // TODO: implement operator priority

            for (int i = 0; i < tokens.Count; i++)
            {
                var currentToken = tokens[i];
                if (currentToken.Type == TokenType.Operator)
                {
                    var operatorToken = (ValueToken)currentToken;

                    var operatorDef = (OperatorDefinition)null; // get defs

                    // '+' and '-' don't error when missing left value
                    // !operatorToken.ValueEqualTo('+') &&
                    // !operatorToken.ValueEqualTo('-')
                    
                    if (operatorDef != null &&
                        operatorDef.RequiresBothSides)
                    {
                        if (i - 1 < 0)
                            return ResultCode.OperatorMissingLeftValue;
                    }

                    if (i + 1 >= tokens.Count)
                        return ResultCode.OperatorMissingRightValue;

                    var leftToken = i - 1 < 0 ? null : tokens[i - 1];
                    var rightToken = tokens[i + 1];

                    var resultList = new List<Token>(leftToken == null ? 2 : 3);
                    if (leftToken != null)
                        resultList.Add(leftToken);
                    resultList.Add(operatorToken);
                    resultList.Add(rightToken);

                    // TODO: check if this yields correct behavior

                    var resultToken = new ListToken(resultList);
                    int firstIndex = i - (resultList.Count - 2);
                    tokens.RemoveRange(firstIndex + 1, resultList.Count - 1);
                    tokens[firstIndex] = resultToken; // insert result token
                    i--; // go back and check for next operator
                }
            }

            return ResultCode.Ok;
        }

        #endregion
    }
}
