using System;
using System.Collections.Generic;
using System.Text;
using Miniräknare.Expressions.Tokens;

namespace Miniräknare.Expressions
{
    public static class ExpressionParser
    {
        public enum ParseCode
        {
            Unknown,
            Ok,
            MismatchedParentheses,

            RepeatingDecimalNumbers,
            RepeatingDecimalSeparators,
            RepeatingListSeparators
        }

        private static TokenType[] DecimalSeparatorTypes =
            new[] { TokenType.DecimalSeparator };

        private static TokenType[] DecimalNumberComponents =
            new[] { TokenType.DecimalDigit, TokenType.DecimalSeparator };

        public const char DecimalSeparator = '.';

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        // https://en.wikipedia.org/wiki/Shunting-yard_algorithm
        /// </remarks>
        /// <param name="tree"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        public static ParseCode Parse(IExpressionTree tree, out List<Token> output)
        {
            output = new List<Token>(tree.Tokens);
            var builder = new StringBuilder();
            var mergeCode = MergeGroupsOfSingles(builder, output);
            if (mergeCode != ParseCode.Ok)
                return mergeCode;

            var outputQueue = new Queue<Token>();
            var opStack = new Stack<Token>();

            for (int i = 0; i < output.Count; i++)
            {
                var token = output[i];
                if (token.Type == TokenType.DecimalNumber ||
                    token.Type == TokenType.DecimalDigit)
                {
                    outputQueue.Enqueue(token);
                }
                else if (token.Type == TokenType.Function)
                {
                    opStack.Push(token);
                }
                else if (token.Type == TokenType.Operator)
                {
                    bool TryPop()
                    {
                        if (opStack.Count == 0)
                            return false;

                        var peek = opStack.Peek();
                        if (peek.Type == TokenType.ListStart)
                            return false;

                        if (peek.Type == TokenType.Function)
                            return true;

                        var opToken = (ValueToken)token;
                        if (peek.Type == TokenType.Operator)
                        {
                            var peekOpToken = (ValueToken)peek;
                            var peekDef = tree.ExpressionOptions.GetOperatorDefinition(peekOpToken.Value);
                            var opDef = tree.ExpressionOptions.GetOperatorDefinition(opToken.Value);

                            if (peekDef.Precedence > opDef.Precedence)
                                return true;

                            if (peekDef.Precedence == opDef.Precedence &&
                                opDef.Associativity == OperatorAssociativity.Left)
                                return true;
                        }
                        return false;
                    }

                    while (TryPop())
                    {
                        outputQueue.Enqueue(opStack.Pop());
                    }
                    opStack.Push(token);
                }
                else if (token.Type == TokenType.ListStart)
                {
                    opStack.Push(token);
                }
                else if (token.Type == TokenType.ListEnd)
                {
                    bool TryPop()
                    {
                        if (opStack.Count > 0)
                        {
                            var peek = opStack.Peek();
                            if (peek.Type != TokenType.ListStart)
                                return true;
                        }
                        return false;
                    }

                    while (TryPop())
                        outputQueue.Enqueue(opStack.Pop());

                    // If the stack runs out without finding a left paren, then there are mismatched parentheses.
                    if (opStack.Count > 0 && opStack.Peek().Type == TokenType.ListStart)
                        opStack.Pop();
                    else
                        return ParseCode.MismatchedParentheses;
                }
                else
                {
                    // ?? what to do here
                }
            }

            // After while loop, if operator stack not null, pop everything to output queue 
            while (opStack.Count > 0)
            {
                var popped = opStack.Pop();

                // If the operator token on the top of the stack is a paren, then there are mismatched parentheses.
                if (popped.Type == TokenType.ListStart ||
                    popped.Type == TokenType.ListEnd)
                    return ParseCode.MismatchedParentheses;

                outputQueue.Enqueue(popped);
            }

            output.Clear();
            output.AddRange(outputQueue);

            return ParseCode.Ok;
        }

        private static ParseCode MergeGroupsOfSingles(StringBuilder builder, List<Token> tokens)
        {
            for (int i = 0; i < tokens.Count; i++)
            {
                #region MergeGroup helpers

                bool CanMergeGroup(TokenType expectedType, out int length)
                {
                    // TODO: cache predicate
                    int end = AsLongAsMatch(tokens, i, t => t.Type == expectedType);
                    length = end - i;
                    if (length > 1)
                        return true;
                    return false;
                }

                void MergeGroupAndInsert(TokenType expectedType, TokenType resultType)
                {
                    if (!CanMergeGroup(expectedType, out int length))
                        return;

                    if (!MergeGroup(builder, tokens, i, length, true, resultType, out var mergedToken))
                        // Should never be thrown as we check the length with CanMergeGroup()
                        throw new Exception("Unknown state.");

                    tokens.Insert(i, mergedToken);
                }

                #endregion

                if (CanMergeGroup(TokenType.DecimalNumber, out _))
                    return (ParseCode.RepeatingDecimalNumbers);

                if (CanMergeGroup(TokenType.DecimalSeparator, out _))
                    return (ParseCode.RepeatingDecimalSeparators);

                if (CanMergeGroup(TokenType.ListSeparator, out _))
                    return (ParseCode.RepeatingListSeparators);

                MergeGroupAndInsert(TokenType.DecimalDigit, TokenType.DecimalNumber);
            }
            return ParseCode.Ok;
        }

        public static int AsLongAsMatch(
            IList<Token> tokens, int offset, Predicate<Token> predicate)
        {
            for (; offset < tokens.Count; offset++)
            {
                if (!predicate.Invoke(tokens[offset]))
                    break;
            }
            return offset;
        }

        private static bool MergeGroup(
            StringBuilder builder, List<Token> tokens, int offset, int length, bool removeTokens,
            TokenType resultType, out Token resultToken)
        {
            if (length > 1)
            {
                builder.Clear();
                for (int i = 0; i < length; i++)
                {
                    var groupToken = tokens[i + offset];
                    if (!(groupToken is ValueToken groupValueToken))
                        throw new Exception("Groups may only consist out of value tokens.");

                    if (groupValueToken.Type == TokenType.DecimalSeparator)
                        builder.Append(DecimalSeparator);
                    else
                        builder.Append(groupValueToken.Value);

                    if (!removeTokens)
                        offset++;
                }

                if (removeTokens)
                    tokens.RemoveRange(offset, length);

                resultToken = new ValueToken(null, resultType, builder.ToString().AsMemory());
                return true;
            }
            resultToken = default;
            return false;
        }
    }
}
