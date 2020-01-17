using System;

namespace Miniräknare.Expressions
{
    public readonly struct Evaluation : IEquatable<Evaluation>
    {
        public static Evaluation Undefined { get; } = new Evaluation(EvalCode.Undefined);
        public static Evaluation Empty { get; } = new Evaluation(EvalCode.Empty);

        public EvalCode Code { get; }
        public UnionValue Value { get; }
        public ReadOnlyMemory<char> UnresolvedName { get; }

        public Evaluation(EvalCode code, ReadOnlyMemory<char> unresolvedName)
        {
            Code = code;
            Value = UnionValue.Null;
            UnresolvedName = unresolvedName;
        }

        public Evaluation(EvalCode code) : this(code, ReadOnlyMemory<char>.Empty)
        {
        }

        public Evaluation(UnionValue result)
        {
            Code = EvalCode.Ok;
            Value = result;
            UnresolvedName = ReadOnlyMemory<char>.Empty;
        }

        public bool Equals(Evaluation other)
        {
            return Code == other.Code
                && Value.Equals(other.Value)
                && UnresolvedName.Span.SequenceEqual(other.UnresolvedName.Span);
        }

        public static implicit operator Evaluation(double value) => new Evaluation(value);
        public static implicit operator Evaluation(float value) => new Evaluation(value);
    }
}
