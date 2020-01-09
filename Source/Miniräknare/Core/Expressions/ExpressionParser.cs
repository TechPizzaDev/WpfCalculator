using System;
using System.Collections.Generic;
using Miniräknare.Expressions.Tokens;

namespace Miniräknare.Expressions
{
    public partial class ExpressionParser
    {
        private static List<OperatorDefinition> _definitions;

        static ExpressionParser()
        {
            _definitions = new List<OperatorDefinition>()
            {
                new OperatorDefinition("+".AsMemory(), false, 0),
                new OperatorDefinition("-".AsMemory(), false, 0),
            };
        }

        public static OperatorDefinition GetOperatorDefinition(ReadOnlySpan<char> value)
        {
            for (int i = 0; i < _definitions.Count; i++)
            {
                var def = _definitions[i];
                if (def.Name.Span.SequenceEqual(value))
                    return def;
            }
            return null;
        }

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

        private static ResultCode MakeOperations(List<Token> tokens)
        {
            if (tokens.Count == 2)
                return ResultCode.Ok;

            // TODO: implement operator priority

            for (int i = 0; i < tokens.Count; i++)
            {
                ResultCode tmpCode; 

                var token = tokens[i];
                if (token.Type == TokenType.List)
                {
                    if ((tmpCode = MakeOperations(((ListToken)token).Children)) != ResultCode.Ok)
                        return tmpCode;
                }
                else if (token.Type == TokenType.Operator)
                {
                    var opToken = (ValueToken)token;

                    var opDef = GetOperatorDefinition(opToken.Value.Span);
                    if (OperatorDefinition.GetRequiresBothSides(opDef))
                    {
                        if (i - 1 < 0)
                            return ResultCode.OperatorMissingLeftValue;
                    }

                    if (i + 1 >= tokens.Count)
                        return ResultCode.OperatorMissingRightValue;

                    var leftToken = i - 1 < 0 ? null : tokens[i - 1];
                    var rightToken = tokens[i + 1];

                    if (rightToken is ListToken rightListToken)
                    {
                        if ((tmpCode = MakeOperations(rightListToken.Children)) != ResultCode.Ok)
                            return tmpCode;
                    }

                    var resultList = new List<Token>(leftToken == null ? 2 : 3);
                    if (leftToken != null)
                        resultList.Add(leftToken);
                    resultList.Add(opToken);
                    resultList.Add(rightToken);

                    var resultToken = new ListToken(resultList);
                    int firstIndex = i - (resultList.Count - 2);
                    tokens.RemoveRange(firstIndex + 1, resultList.Count - 1);
                    tokens[firstIndex] = resultToken;

                    i--; // go back and check for next operator
                }
            }

            return ResultCode.Ok;
        }

        #endregion
    }
}
