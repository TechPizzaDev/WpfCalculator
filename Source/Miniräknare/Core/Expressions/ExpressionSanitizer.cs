using System;
using System.Collections.Generic;
using System.Text;
using static Miniräknare.Expressions.ExpressionTokenizer;

namespace Miniräknare.Expressions
{
    public partial class ExpressionSanitizer
    {
        public static SanitizeResult SanitizeTokens(IList<Token> tokens)
        {
            var result = RemoveSpacesInGroups(tokens);
            if (result.Code != ResultCode.Ok)
                return result;

            MergeGroups(tokens);
            RemoveWhiteSpaces(tokens);

            return result;
        }

        private static SanitizeResult RemoveSpacesInGroups(IList<Token> tokens)
        {
            var lastType = TokenType.Unknown;
            int charPosition = 0;

            for (int i = 0; i < tokens.Count; i++)
            {
                var currentToken = tokens[i];
                var currentType = currentToken.Type;

                bool IncludeInGroup(
                    TokenType groupType,
                    TokenType spaceType)
                {
                    if (lastType != currentType &&
                        lastType == groupType)
                    {
                        if (currentType == spaceType)
                        {
                            var nextToken = i + 1 < tokens.Count ? tokens[i + 1] : default;
                            if (nextToken.Type == groupType)
                                return false;
                        }
                    }
                    return true;
                }

                if (!IncludeInGroup(TokenType.Name, TokenType.WhiteSpace))
                    return new SanitizeResult(ResultCode.WhiteSpaceInName, i, charPosition);

                // Only number literals have spaces that can be removed.
                if (!IncludeInGroup(TokenType.NumberLiteral, TokenType.Space))
                    tokens.RemoveAt(i--);

                lastType = currentType;
                charPosition += currentToken.Value.Length;
            }
            return new SanitizeResult(ResultCode.Ok, null, null);
        }

        private static void MergeGroups(IList<Token> tokens)
        {
            var lastType = TokenType.Unknown;
            int lastGroupIndex = 0;
            var groupBuilder = new StringBuilder();

            int i = 0;
            while (i < tokens.Count)
            {
                var currentToken = tokens[i];
                var currentType = currentToken.Type;

                if (lastType != currentType)
                {
                    if (lastType == TokenType.NumberLiteral)
                    {
                        int groupLength = i - lastGroupIndex;
                        if (groupLength > 1)
                        {
                            // Concat the group values...
                            while (lastGroupIndex < i)
                            {
                                var groupToken = tokens[lastGroupIndex];
                                groupBuilder.Append(groupToken.Value);

                                tokens.RemoveAt(lastGroupIndex);
                                i--;
                            }

                            string groupValue = groupBuilder.ToString();
                            groupBuilder.Clear();

                            // and insert the group as one token.
                            tokens.Insert(lastGroupIndex, new Token(lastType, groupValue.AsMemory()));
                            i++;
                        }
                    }

                    lastGroupIndex = i;
                    lastType = currentType;
                }
                i++;
            }
        }

        private static void RemoveWhiteSpaces(IList<Token> tokens)
        {
            int i = 0;
            while (i < tokens.Count)
            {
                var currentToken = tokens[i];
                if (currentToken.Type == TokenType.WhiteSpace)
                    tokens.RemoveAt(i);
                else
                    i++;
            }
        }

        public enum ResultCode
        {
            Ok = 0,
            WhiteSpaceInName
        }

        public readonly struct SanitizeResult
        {
            public ResultCode Code { get; }
            public int? ErrorTokenPosition { get; }
            public int? ErrorCharPosition { get; }

            public SanitizeResult(
                ResultCode code, int? errorTokenPosition, int? errorCharPosition)
            {
                Code = code;
                ErrorTokenPosition = errorTokenPosition;
                ErrorCharPosition = errorCharPosition;
            }
        }

    }
}
