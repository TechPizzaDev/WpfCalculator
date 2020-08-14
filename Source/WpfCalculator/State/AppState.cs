using System;
using System.Collections.Generic;
using System.Linq;
using WpfCalculator.Expressions;

namespace WpfCalculator
{
    public class AppState
    {
        public Dictionary<ReadOnlyString, ExpressionBox> Expressions { get; } =
            new Dictionary<ReadOnlyString, ExpressionBox>();



        public bool IsValidName(ReadOnlyString newName, out ReadOnlyString validatedName)
        {
            validatedName = newName.Chars.Trim();

            if (validatedName.IsEmpty)
                return true;

            var nameSpan = validatedName.Span;
            for (int i = 0; i < nameSpan.Length; i++)
            {
                char c = nameSpan[i];
                if (!(
                    ExpressionTokenizer.IsNameToken(c) ||
                    ExpressionTokenizer.IsSpaceToken(c) ||
                    (i > 0 && ExpressionTokenizer.IsDigitToken(c))))
                    return false;
            }

            if (Expressions.ContainsKey(validatedName))
                return false;

            return true;
        }

        public void EvaluateErroredExpressions()
        {
            var evaluatedSet = new HashSet<ExpressionBox>();

            // TODO: stackify

            void Body(ExpressionBox box)
            {
                if (box.Error == null)
                    return;

                foreach (var reference in box.References.ToArray())
                    Body(reference);

                if (evaluatedSet.Add(box))
                    box.Evaluate(isReevaluatingState: true);
            }

            foreach (var expression in Expressions.Values)
                Body(expression);
        }

        public ReadOnlyString GenerateFieldName()
        {
            const int maxLength = 10;
            var alphabet = App.LatinAlphabet;
            int targetIndex = alphabet.Length;

            int length = 1;
            int lastIndex = maxLength - 1;

            var nameBuffer = new char[maxLength];
            Span<int> indices = stackalloc int[maxLength];
            Memory<char> name;

            bool tryGet = true;
            do
            {
                for (int i = 0; i < length; i++)
                    nameBuffer[nameBuffer.Length - 1 - i] = alphabet[indices[indices.Length - 1 - i]];
                name = nameBuffer.AsMemory(nameBuffer.Length - length, length);

                if (!Expressions.ContainsKey(name))
                    break;

                indices[^1]++;

                for (int i = indices.Length; i-- > 0;)
                {
                    if (indices[i] != targetIndex)
                        continue;

                    if (i - 1 < 0)
                    {
                        if (indices[i] == targetIndex)
                        {
                            tryGet = false;
                            break;
                        }
                    }
                    else
                    {
                        indices[i - 1]++;
                    }

                    if (i < lastIndex)
                    {
                        length++;
                        if (length > maxLength)
                            throw new Exception("Max name length reached.");

                        lastIndex = i;
                    }
                    indices[i] = 0;
                }
            } while (tryGet);

            return name;
        }
    }
}
