using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Miniräknare.Expressions.Tokens
{
    public abstract class CollectionToken : Token, IList<Token>
    {
        public List<Token> Children { get; }

        public int Count => Children.Count;

        public Token this[int index]
        {
            get => Children[index];
            set => Children[index] = value;
        }

        internal override string DebuggerDisplay
        {
            get
            {
                var builder = new StringBuilder();
                builder.Append(base.DebuggerDisplay);
                builder.Append(" (").Append(Count).Append("): \"");

                ToStringCore(builder, false);

                builder.Append("\"");
                return builder.ToString();
            }
        }

        public CollectionToken(TokenType type, List<Token> children) : base(type)
        {
            Children = children ?? throw new ArgumentNullException(nameof(children));
        }

        public void RemoveRange(int index, int count)
        {
            Children.RemoveRange(index, count);
        }

        #region ToString

        public override string ToString()
        {
            return ToStringCore(new StringBuilder(), true).ToString();
        }

        public virtual StringBuilder ToStringCore(StringBuilder builder, bool enclose)
        {
            if (enclose)
                builder.Append(ExpressionTokenizer.ListOpeningChar);
            
            for (int i = 0; i < Children.Count; i++)
            {
                var token = Children[i];
                if (token is ListToken listToken)
                    listToken.ToStringCore(builder, true);
                else
                    builder.Append(token.ToString());

                if (token.Type == TokenType.ListSeparator && i < Children.Count - 1)
                    builder.Append(' ');
            }

            if (enclose)
                builder.Append(ExpressionTokenizer.ListClosingChar);

            return builder;
        }

        #endregion

        #region IList

        public bool IsReadOnly => false;

        public int IndexOf(Token item) => Children.IndexOf(item);
        public void Insert(int index, Token item) => Children.Insert(index, item);
        public void RemoveAt(int index) => Children.RemoveAt(index);
        public void Add(Token item) => Children.Add(item);
        public void Clear() => Children.Clear();
        public bool Contains(Token item) => Children.Contains(item);
        public void CopyTo(Token[] array, int arrayIndex) => Children.CopyTo(array, arrayIndex);
        public bool Remove(Token item) => Children.Remove(item);

        #endregion

        public List<Token>.Enumerator GetEnumerator() => Children.GetEnumerator();
        IEnumerator<Token> IEnumerable<Token>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
