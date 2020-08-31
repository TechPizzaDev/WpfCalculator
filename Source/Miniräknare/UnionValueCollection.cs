using System;

namespace Miniräknare
{
    public readonly struct UnionValueCollection
    {
        public UnionValue? Child { get; }
        public UnionValueCollection[] Children { get; }

        public UnionValueCollection(UnionValue? child)
        {
            Child = child;
            Children = default;
        }

        public UnionValueCollection(UnionValueCollection[] children)
        {
            Child = default;
            Children = children;
        }

        public static implicit operator UnionValueCollection(UnionValue value)
        {
            return new UnionValueCollection(value);
        }

        public static implicit operator UnionValueCollection(UnionValueCollection[] children)
        {
            return new UnionValueCollection(children);
        }
    }
}