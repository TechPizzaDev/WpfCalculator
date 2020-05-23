using System;

namespace WpfCalculator.Expressions
{
    public readonly struct Evaluation : IEquatable<Evaluation>
    {
        public static Evaluation Undefined { get; } = new Evaluation(EvalCode.Undefined);

        public EvalCode Code { get; }
        public UnionValueCollection Values { get; }
        public ReadOnlyMemory<char> UnresolvedName { get; }

        public Evaluation(
            EvalCode code, UnionValueCollection values, ReadOnlyMemory<char> unresolvedName)
        {
            Code = code;
            Values = values;
            UnresolvedName = unresolvedName;
        }

        public Evaluation(EvalCode code, UnionValueCollection values) :
            this(code, values, ReadOnlyMemory<char>.Empty)
        {
        }

        public Evaluation(EvalCode code, ReadOnlyMemory<char> unresolvedName)
            : this(code, default, unresolvedName)
        {
        }

        public Evaluation(EvalCode code) : this(code, ReadOnlyMemory<char>.Empty)
        {
        }

        public Evaluation(UnionValueCollection values) 
            : this(EvalCode.Ok, values, ReadOnlyMemory<char>.Empty)
        {
        }

        public bool Equals(Evaluation other)
        {
            return Code == other.Code
                && Values.Equals(other.Values)
                && UnresolvedName.Span.SequenceEqual(other.UnresolvedName.Span);
        }

        public static implicit operator Evaluation(double value)
        {
            return new Evaluation(new UnionValue(value));
        }

        public static implicit operator Evaluation(float value)
        {
            return new Evaluation(new UnionValue(value));
        }
    }
}
