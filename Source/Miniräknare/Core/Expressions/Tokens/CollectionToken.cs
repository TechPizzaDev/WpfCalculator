using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Miniräknare.Expressions.Tokens
{
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}", Name = "{Type}")]
    public abstract class CollectionToken : Token, IList<Token>
    {
        public List<Token> Children { get; }

        public int Count => Children.Count;

        public Token this[int index]
        {
            get => Children[index];
            set => Children[index] = value;
        }

        public CollectionToken(TokenType type, List<Token> children) : base(type)
        {
            Children = children ?? throw new ArgumentNullException(nameof(children));
        }

        #region ToString

        public override string ToString()
        {
            return ToStringCore().ToString();
        }

        private string GetDebuggerDisplay()
        {
            var builder = ToStringCore();
            string prefix = "(" + Count + ")";
            if (builder.Length > 0)
                builder.Insert(0, prefix).Insert(prefix.Length, " ");
            return builder.ToString();
        }

        protected virtual StringBuilder ToStringCore()
        {
            var builder = new StringBuilder();
            builder.Append(ExpressionTokenizer.ListStartChar);

            foreach (var token in Children)
                builder.Append(token.ToString());

            builder.Append(ExpressionTokenizer.ListEndChar);
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
