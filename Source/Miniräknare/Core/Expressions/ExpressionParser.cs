using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
            OperatorOnOperator,
            InvalidTokenBeforeList,
            MissingMultiplicationDefinition,
            UnknownSymbol,
            EmptyList // TODO
        }

        public static ResultCode Parse(ExpressionOptions options, List<Token> tokens)
        {
            if (tokens == null) throw new ArgumentNullException(nameof(tokens));
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (tokens.Count == 0)
                return ResultCode.NoTokens;

            // TODO: check for valid placement of list separators

            ResultCode code;
            if ((code = MakeLists(tokens)) != ResultCode.Ok ||
                (code = MakeFunctions(options, tokens)) != ResultCode.Ok ||
                (code = MakeImplicitMultiplications(options, tokens)) != ResultCode.Ok ||
                (code = MakeOperatorGroups(options, tokens)) != ResultCode.Ok ||
                (code = AssignParents(options, tokens)) != ResultCode.Ok)
                return code;
            return code;
        }

        public static ResultCode Parse(ExpressionTree tree)
        {
            return Parse(tree.ExpressionOptions, tree.Tokens.Children);
        }

        #region MakeLists

        private static ResultCode MakeLists(IList<Token> tokens)
        {
            var listStack = new List<List<Token>>();
            int ListDepth() => listStack.Count - 1;

            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                var currentType = token.Type;

                if (currentType == TokenType.ListOpening)
                {
                    listStack.Add(new List<Token>());

                    // Remove the ListStart token.
                    tokens.RemoveAt(i--);
                }
                else if (currentType == TokenType.ListClosing)
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
                        listStack[ListDepth()].Add(token);
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

        private static ResultCode MakeFunctions(ExpressionOptions options, List<Token> tokens)
        {
            var stack = new Stack<List<Token>>();
            stack.Push(tokens);

            var argumentAccumulator = new List<Token>();
            var argumentListAccumulator = new List<Token>();

            while (stack.Count > 0)
            {
                bool hasList = false;
                var list = stack.Pop();

                for (int i = list.Count; i-- > 0;)
                {
                    var token = list[i];

                    if (hasList)
                    {
                        hasList = false;

                        if (token.Type == TokenType.Operator ||
                            token.Type == TokenType.DecimalDigit ||
                            token.Type == TokenType.DecimalNumber ||
                            token.Type == TokenType.List)
                            continue;

                        if (token.Type != TokenType.Name)
                            return ResultCode.InvalidTokenBeforeList;

                        void FlushArgumentAccumulator()
                        {
                            if (argumentAccumulator.Count == 0)
                                return;

                            argumentListAccumulator.Add(new ListToken(argumentAccumulator));
                            argumentAccumulator = new List<Token>();
                        }

                        var arguments = (ListToken)list[i + 1];
                        foreach (var argument in arguments)
                        {
                            if (argument.Type == TokenType.ListSeparator)
                            {
                                FlushArgumentAccumulator();
                                argumentListAccumulator.Add(argument);
                            }
                            else
                            {
                                argumentAccumulator.Add(argument);
                            }
                        }
                        FlushArgumentAccumulator();

                        var name = (ValueToken)token;
                        var func = new FunctionToken(name, argumentListAccumulator);
                        argumentListAccumulator = new List<Token>();

                        list[i] = func;
                        list.RemoveAt(i + 1);
                    }

                    if (token is ListToken listToken)
                    {
                        hasList = true;
                        stack.Push(listToken.Children);
                    }
                }
            }

            return ResultCode.Ok;
        }

        #endregion

        #region MakeImplicitMultiplications

        private static ResultCode MakeImplicitMultiplications(
            ExpressionOptions options, List<Token> tokens)
        {
            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                if (token is CollectionToken collectionToken)
                {
                    var code = MakeImplicitMultiplications(options, collectionToken.Children);
                    if (code != ResultCode.Ok)
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
                    leftToken.Type == TokenType.Name ||
                    leftToken.Type == TokenType.ListSeparator)
                    continue;

                if (leftToken.Type != TokenType.DecimalDigit &&
                    leftToken.Type != TokenType.DecimalNumber &&
                    leftToken.Type != TokenType.List)
                    return ResultCode.InvalidTokenBeforeList;

                var multiplyOpDef = options.GetOperatorDefinition(OperatorType.Multiply);
                if (multiplyOpDef == null)
                    return ResultCode.MissingMultiplicationDefinition;

                var opToken = new ValueToken(TokenType.Operator, multiplyOpDef.Names[0]);
                tokens.Insert(i, opToken);
            }
            return ResultCode.Ok;
        }

        #endregion

        #region MakeOperatorGroups

        private static ResultCode MakeOperatorGroups(ExpressionOptions options, List<Token> tokens)
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
                        listStack.Push(collectionToken.Children);
                }

                // Gather operators so we can sort them by priority rules.
                opIndices.Clear();
                for (int j = 0; j < currentTokens.Count; j++)
                {
                    var token = currentTokens[j];
                    if (token.Type != TokenType.Operator)
                        continue;

                    var opToken = (ValueToken)token;
                    var opDef = options.GetOperatorDefinition(opToken.Value);
                    if (opDef == null)
                        return ResultCode.UnknownSymbol;

                    opIndices.Add((index: j, opToken, opDef));
                }

                opIndices.Sort((x, y) =>
                {
                    int xPriority = x.definition?.Precedence ?? 0;
                    int yPriority = y.definition?.Precedence ?? 0;

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
                    if (opDef?.Associativity != OperatorSidedness.Right)
                    {
                        if (left < 0)
                        {
                            if (opDef != null && opDef.Associativity.HasFlag(OperatorSidedness.Left))
                                return ResultCode.OperatorMissingLeftValue;
                        }
                        else
                        {
                            leftToken = currentTokens[left];
                        }
                    }
                    if (leftToken?.Type == TokenType.Operator)
                        continue;

                    int right = opIndex + 1;
                    if (opDef?.Associativity != OperatorSidedness.Left)
                    {
                        if (right >= currentTokens.Count)
                        {
                            if ((bool)opDef?.Associativity.HasFlag(OperatorSidedness.Right))
                                return ResultCode.OperatorMissingRightValue;
                        }
                        else
                        {
                            rightToken = currentTokens[right];
                            if (rightToken.Type == TokenType.Operator)
                                continue;

                            // Mitigates operators with following operators.
                            if (rightToken.Type == TokenType.Operator)
                            {
                                int secondRight = opIndex + 2;
                                if (secondRight < currentTokens.Count)
                                {
                                    var subToken = new ListToken(new List<Token>(2)
                                    {
                                        rightToken,
                                        currentTokens[secondRight]
                                    });

                                    rightToken = subToken;
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

                    // Try to skip making a 1-item list.
                    int resultCount = 1 + sideTokenCount;
                    if (resultCount == currentTokens.Count)
                        continue;

                    var resultList = new List<Token>(resultCount);
                    resultList.AddNonNull(leftToken);
                    resultList.Add(opToken);
                    resultList.AddNonNull(rightToken);

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

        #region AssignParents

        public static ResultCode AssignParents(ExpressionOptions options, List<Token> root)
        {
            var stack = new Stack<CollectionToken>();

            // Tokens in the root list have 'null' parent.
            foreach (var token in root)
                if (token is CollectionToken collection)
                    stack.Push(collection);

            while (stack.Count > 0)
            {
                var parent = stack.Pop();
                foreach (var token in parent)
                {
                    token.Parent = parent;

                    if (token is CollectionToken collection)
                        stack.Push(collection);
                }
            }

            return ResultCode.Ok;
        }

        #endregion
    }
}
