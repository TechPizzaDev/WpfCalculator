using System;
using System.Collections.Generic;
using Miniräknare.Expressions.Tokens;

namespace Miniräknare.Expressions
{
    public static class ExpressionParser
    {
        // TODO: remove recursion

        public enum ResultCode
        {
            Ok = 0,
            NoTokens,
            MissingListEnd,
            ListEndWithoutStart,
            OperatorMissingLeftValue,
            OperatorMissingRightValue,
            InvalidTokenBeforeList,
            MissingMultiplicationOperator,
            UnknownSymbol,
            EmptyList // TODO
        }

        public static ResultCode Parse(List<Token> tokens, ExpressionOptions options)
        {
            if (tokens == null) throw new ArgumentNullException(nameof(tokens));
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (tokens.Count == 0)
                return ResultCode.NoTokens;

            ResultCode code;
            if ((code = MakeLists(tokens)) != ResultCode.Ok ||
                (code = MakeFunctions(tokens, options)) != ResultCode.Ok ||
                (code = MakeImplicitMultiplications(tokens, options)) != ResultCode.Ok ||
                (code = MakeOperatorGroups(tokens, options)) != ResultCode.Ok)
                return code;
            return code;
        }

        public static ResultCode Parse(ExpressionTree tree)
        {
            return Parse(tree.Tokens, tree.Options);
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

        private static ResultCode MakeFunctions(List<Token> tokens, ExpressionOptions options)
        {
            // Loop from the end so we can call "MakeFunctions" recursively.
            for (int i = tokens.Count; i-- > 0;)
            {
                var token = tokens[i];
                if (token.Type != TokenType.List)
                    continue;

                var listToken = (ListToken)token;
                ResultCode code;
                if ((code = MakeFunctions(listToken.Children, options)) != ResultCode.Ok)
                    return code;

                if (i - 1 < 0)
                    continue; // We reached the list's beginning.

                var leftToken = tokens[i - 1];
                if (leftToken.Type != TokenType.Name)
                {
                    if (leftToken.Type == TokenType.Operator ||
                        leftToken.Type == TokenType.DecimalDigit ||
                        leftToken.Type == TokenType.DecimalNumber)
                        continue;
                    return ResultCode.InvalidTokenBeforeList;
                }

                var nameToken = (ValueToken)leftToken;
                var funcToken = new FunctionToken(nameToken, listToken);

                tokens[i - 1] = funcToken; // replace left token
                tokens.RemoveAt(i); // remove current token
            }
            return ResultCode.Ok;
        }

        #endregion

        #region MakeImplicitMultiplications

        private static ResultCode MakeImplicitMultiplications(
            List<Token> tokens, ExpressionOptions options)
        {
            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                if (token is CollectionToken collectionToken)
                {
                    ResultCode code;
                    if ((code = MakeImplicitMultiplications(collectionToken.Children, options)) != ResultCode.Ok)
                        return code;
                }
                else if (token.Type != TokenType.Name)
                {
                    // Skip as this type is not allowed to have an implicit factor prefix.
                    continue;
                }

                if (i - 1 < 0)
                    continue; // We are at the list's beginning.

                var leftToken = tokens[i - 1];
                if (leftToken.Type == TokenType.Operator ||
                    leftToken.Type == TokenType.Name)
                    continue;

                if (leftToken.Type != TokenType.List &&
                    leftToken.Type != TokenType.DecimalDigit &&
                    leftToken.Type != TokenType.DecimalNumber)
                    return ResultCode.InvalidTokenBeforeList;

                var multiplyOpDef = options.GetOperatorDefinition(OperatorType.Multiply);
                if (multiplyOpDef == null)
                    return ResultCode.MissingMultiplicationOperator;

                var opToken = new ValueToken(TokenType.Operator, multiplyOpDef.Names[0]);
                tokens.Insert(i, opToken);
            }
            return ResultCode.Ok;
        }

        #endregion

        #region MakeOperationsGroups

        private static ResultCode MakeOperatorGroups(List<Token> tokens, ExpressionOptions options)
        {
            var listStack = new Stack<List<Token>>();
            listStack.Push(tokens);

            var opIndices = new List<(int index, ValueToken token, OperatorDefinition definition)>();
            var opShifts = new List<(int index, int shift)>();

            while (listStack.Count > 0)
            {
                var currentTokens = listStack.Pop();
                for (int j = 0; j < currentTokens.Count; j++)
                {
                    var token = currentTokens[j];
                    if (token is CollectionToken collectionToken)
                    {
                        listStack.Push(collectionToken.Children);
                    }
                }

                // Gather operators so we can sort them by priority rules.
                opIndices.Clear();
                for (int j = 0; j < currentTokens.Count; j++)
                {
                    var token = currentTokens[j];
                    if (token.Type != TokenType.Operator)
                        continue;

                    var opToken = (ValueToken)token;
                    var opDef = options.GetOperatorDefinition(opToken.Value.Span);
                    if (opDef == null)
                        return ResultCode.UnknownSymbol;

                    opIndices.Add((index: j, opToken, opDef));
                }

                opIndices.Sort((x, y) =>
                {
                    int xPriority = x.definition?.Priority ?? 0;
                    int yPriority = y.definition?.Priority ?? 0;

                    // Sort types in descending order.
                    int priorityCompare = yPriority.CompareTo(xPriority);
                    if (priorityCompare != 0)
                        return priorityCompare;

                    // Sort indices of same type in ascending order.
                    return x.index.CompareTo(y.index);
                });

                // Merge token triplets with a center operator or
                // pairs with a leading operator.
                opShifts.Clear();
                for (int i = 0; i < opIndices.Count; i++)
                {
                    var (opIndex, opToken, opDef) = opIndices[i];

                    // Offset "opIndex" by shifts caused by previous operator merges.
                    for (int j = 0; j < opShifts.Count; j++)
                    {
                        var (shiftIndex, shift) = opShifts[j];
                        if (shiftIndex < opIndex)
                            opIndex += shift;
                    }

                    Token leftToken = null;
                    Token rightToken = null;

                    int left = opIndex - 1;
                    if (opDef?.Sidedness != OperatorSidedness.Right)
                    {
                        if (left < 0)
                        {
                            if (opDef?.Sidedness == OperatorSidedness.Left ||
                                opDef?.Sidedness == OperatorSidedness.OptionalRight)
                                return ResultCode.OperatorMissingLeftValue;
                        }
                        else
                        {
                            leftToken = currentTokens[left];
                        }
                    }

                    int right = opIndex + 1;
                    if (opDef?.Sidedness != OperatorSidedness.Left)
                    {
                        if (right >= currentTokens.Count)
                        {
                            if (opDef?.Sidedness == OperatorSidedness.Right ||
                                opDef?.Sidedness == OperatorSidedness.OptionalLeft)
                                return ResultCode.OperatorMissingRightValue;
                        }
                        else
                        {
                            rightToken = currentTokens[right];

                            // Mitigates the "op -value" problem.
                            if (rightToken.Type == TokenType.Operator)
                            {
                                int secondRight = opIndex + 2;
                                if (secondRight < currentTokens.Count)
                                {
                                    rightToken = new ListToken(new List<Token>(2)
                                    {
                                        rightToken,
                                        currentTokens[secondRight]
                                    });
                                    currentTokens[right] = rightToken;
                                    currentTokens.RemoveAt(secondRight);

                                    opShifts.Add((right, -1));
                                    opIndices.RemoveAll(x => x.index == right);
                                }
                                else
                                {
                                    return ResultCode.OperatorMissingRightValue;
                                }
                            }
                        }
                    }

                    int sideTokenCount = 0;
                    if (leftToken != null)
                        sideTokenCount++;
                    if (rightToken != null)
                        sideTokenCount++;
                    var resultList = new List<Token>(1 + sideTokenCount);

                    if (leftToken != null)
                        resultList.Add(leftToken);
                    resultList.Add(opToken);
                    if (rightToken != null)
                        resultList.Add(rightToken);

                    int firstIndex = opIndex - (leftToken != null ? 1 : 0);
                    var resultToken = new ListToken(resultList);
                    currentTokens[firstIndex] = resultToken;
                    currentTokens.RemoveRange(firstIndex + 1, resultList.Count - 1);

                    int nextShift = 1 - resultList.Count;
                    opShifts.Add((opIndex, nextShift));
                }
            }
            return ResultCode.Ok;
        }

        #endregion
    }
}
