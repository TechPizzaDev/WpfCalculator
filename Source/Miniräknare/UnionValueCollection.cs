using System;
using System.Collections.Generic;
using System.Linq;

namespace Miniräknare
{
    public readonly struct UnionValueCollection
    {
        public static UnionValueCollection Empty { get; } = default;

        private readonly UnionValue _value;
        private readonly UnionValue[] _values;

        public UnionValue First => Values.Length == 0 ? _value : Values[0];
        public UnionValue[] Values => _values ?? Array.Empty<UnionValue>();

        public int Length => _values == null ? 1 : Values.Length;

        public UnionValueCollection(UnionValue value) : this()
        {
            _value = value;
        }

        public UnionValueCollection(ReadOnlySpan<UnionValue> values) : this()
        {
            _values = values.ToArray();
        }

        public UnionValueCollection(IEnumerable<UnionValue> values) : this()
        {
            _values = values.ToArray();
        }

        public static implicit operator UnionValueCollection(UnionValue value)
        {
            return new UnionValueCollection(value);
        }
    }
}