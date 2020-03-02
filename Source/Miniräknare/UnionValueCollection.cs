using System;
using System.Collections.Generic;
using System.Linq;

namespace Miniräknare
{
    public readonly struct UnionValueCollection
    {
        public static UnionValueCollection Empty { get; } = default;

        private readonly UnionValue _first;
        private readonly UnionValue[] _items;

        public UnionValue First => Items.Length == 0 ? _first : Items[0];
        public UnionValue[] Items => _items ?? Array.Empty<UnionValue>();

        public int Length => _items == null ? 1 : Items.Length;

        public UnionValueCollection(UnionValue value) : this()
        {
            _first = value;
        }

        public UnionValueCollection(ReadOnlySpan<UnionValue> values) : this()
        {
            _items = values.ToArray();
        }

        public UnionValueCollection(IEnumerable<UnionValue> values) : this()
        {
            _items = values.ToArray();
        }

        public static implicit operator UnionValueCollection(UnionValue value)
        {
            return new UnionValueCollection(value);
        }
    }
}