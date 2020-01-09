using System;

namespace Miniräknare.Expressions
{
    public enum EvalCode
    {
        Undefined,
        Ok,
        InvalidOperatorCall,
        UnresolvedReference,
        UnresolvedOperator,
        UnresolvedFunction
    }

    public readonly struct Evaluation
    {
        public static Evaluation Undefined { get; } = new Evaluation(
            EvalCode.Undefined, ReadOnlyMemory<char>.Empty);

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

        public static implicit operator Evaluation(double value) => new Evaluation(value);
        public static implicit operator Evaluation(float value) => new Evaluation(value);
    }
}
