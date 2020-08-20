using System;

namespace Miniräknare
{
    public readonly struct UnionValueCollection
    {
        public UnionValue? Child { get; }
        public ReadOnlyMemory<UnionValueCollection> Children { get; }

        public UnionValueCollection(UnionValue? child)
        {
            Child = child;
            Children = default;
        }

        public UnionValueCollection(ReadOnlyMemory<UnionValueCollection> children)
        {
            Child = default;
            Children = children;
        }

        public static implicit operator UnionValueCollection(UnionValue value)
        {
            return new UnionValueCollection(value);
        }

        public static implicit operator UnionValueCollection(ReadOnlyMemory<UnionValueCollection> children)
        {
            return new UnionValueCollection(children);
        }
    }
}