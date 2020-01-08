namespace Miniräknare
{
    public enum UnionValueType : int
    {
        Null = 0,
        Float,
        Double
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
    public readonly struct UnionValue
    {
        public static readonly UnionValue Null = new UnionValue();

        [System.Runtime.InteropServices.FieldOffset(0)]
        public readonly UnionValueType Type;

        [System.Runtime.InteropServices.FieldOffset(sizeof(UnionValueType))]
        public readonly float Float;

        [System.Runtime.InteropServices.FieldOffset(sizeof(UnionValueType))]
        public readonly double Double;

        public UnionValue(double value) : this()
        {
            Type = UnionValueType.Double;
            Double = value;
        }

        public static implicit operator UnionValue(double value) => new UnionValue(value);

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