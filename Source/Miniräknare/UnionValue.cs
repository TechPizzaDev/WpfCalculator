using System.Runtime.InteropServices;

namespace Miniräknare
{
    public enum UnionValueType : int
    {
        Null = 0,
        Float,
        Double
    }

    [StructLayout(LayoutKind.Explicit)]
    public readonly struct UnionValue
    {
        public static UnionValue Null { get; } = new UnionValue(UnionValueType.Null);

        [field: FieldOffset(0)] public UnionValueType Type { get; }
        [field: FieldOffset(sizeof(UnionValueType))] public float Float { get; }
        [field: FieldOffset(sizeof(UnionValueType))] public double Double { get; }

        private UnionValue(UnionValueType type) : this() => Type = type;
        public UnionValue(double value) : this(UnionValueType.Double) => Double = value;
        public UnionValue(float value) : this(UnionValueType.Float) => Float = value;

        public static implicit operator UnionValue(double value) => new UnionValue(value);
        public static implicit operator UnionValue(float value) => new UnionValue(value);

        public override string ToString()
        {
            return Type switch
            {
                UnionValueType.Double => Double.ToString() + "d",
                UnionValueType.Float => Float.ToString() + "f",
                UnionValueType.Null => "null",
                _ => string.Empty,
            };
        }
    }
}