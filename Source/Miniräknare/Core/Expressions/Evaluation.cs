using System;

namespace Miniräknare.Expressions
{
    public readonly struct Evaluation : IEquatable<Evaluation>
    {
        public static Evaluation Undefined { get; } = new Evaluation(EvalCode.Undefined);
        public static Evaluation Empty { get; } = new Evaluation(EvalCode.Empty);

        public EvalCode Code { get; }
        public UnionValueCollection Values { get; }
        public ReadOnlyMemory<char> UnresolvedName { get; }

        public Evaluation(EvalCode code, UnionValueCollection values, ReadOnlyMemory<char> unresolvedName)
        {
            Code = code;
            Values = values;
            UnresolvedName = unresolvedName;
        }

        public Evaluation(EvalCode code, ReadOnlyMemory<char> unresolvedName)
            : this(code, UnionValueCollection.Empty, unresolvedName)
        {
        }

        public Evaluation(EvalCode code, UnionValueCollection values) : this(code, values, ReadOnlyMemory<char>.Empty)
        {
        }

        public Evaluation(EvalCode code) : this(code, ReadOnlyMemory<char>.Empty)
        {
        }

        public Evaluation(UnionValueCollection values)
        {
            Code = EvalCode.Ok;
            Values = values;
            UnresolvedName = ReadOnlyMemory<char>.Empty;
        }

        public bool Equals(Evaluation other)
        {
            return Code == other.Code
                && Values.Equals(other.Values)
                && UnresolvedName.Span.SequenceEqual(other.UnresolvedName.Span);
        }

        public static implicit operator Evaluation(double value) => new Evaluation(new UnionValue(value));
        public static implicit operator Evaluation(float value) => new Evaluation(new UnionValue(value));
    }
}
