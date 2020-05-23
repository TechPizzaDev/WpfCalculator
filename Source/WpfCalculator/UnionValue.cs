using System;
using System.Runtime.InteropServices;

namespace WpfCalculator
{
    using VType = UnionValueType;
    using NGroup = NumberGroup;

    [StructLayout(LayoutKind.Explicit)]
    public readonly struct UnionValue : IEquatable<UnionValue>
    {
        public const int ValueOffset = sizeof(VType) + sizeof(NGroup);

        public static UnionValue Null { get; } = new UnionValue(VType.Null);

        [field: FieldOffset(0)] public VType ValueType { get; }
        [field: FieldOffset(sizeof(VType))] public NGroup NumberGroup { get; }

        [field: FieldOffset(ValueOffset)] public float Float { get; }
        [field: FieldOffset(ValueOffset)] public double Double { get; }
        [field: FieldOffset(ValueOffset)] public long Long { get; }
        [field: FieldOffset(ValueOffset)] public ulong ULong { get; }
        [field: FieldOffset(ValueOffset)] public decimal Decimal { get; }

        public ulong Enum => ULong;

        private UnionValue(VType valueType, NGroup numberGroup) : this()
        {
            NumberGroup = numberGroup;
            ValueType = valueType;
        }

        private UnionValue(VType valueType) : this(valueType, NGroup.Real)
        {
        }

        private UnionValue(VType valueType, NGroup numberGroup, ulong value) :
            this(valueType, numberGroup)
        {
            ULong = value;
        }

        public UnionValue(double value, NGroup numberType = default) : this(VType.Double, numberType) => Double = value;
        public UnionValue(float value, NGroup numberType = default) : this(VType.Float, numberType) => Float = value;
        public UnionValue(long value, NGroup numberType = default) : this(VType.Long, numberType) => Long = value;
        public UnionValue(decimal value, NGroup numberType = default) : this(VType.Decimal, numberType) => Decimal = value;

        public static implicit operator UnionValue(double value) => new UnionValue(value);
        public static implicit operator UnionValue(float value) => new UnionValue(value);

        public static UnionValue FromEnum<TEnum>(TEnum value)
            where TEnum : Enum, IConvertible
        {
            return new UnionValue(VType.Enum, NGroup.Real, value.ToUInt64(null));
        }

        public bool Equals(UnionValue other)
        {
            if (NumberGroup != other.NumberGroup)
                return false;

            switch (ValueType & other.ValueType)
            {
                case VType.Double: return Double == other.Double;
                case VType.Float: return Float == other.Float;
                case VType.Long: return Long == other.Long;

                case VType.Enum:
                case VType.ULong: return ULong == other.ULong;

                case VType.Null:
                default:
                    return ValueType == VType.Null
                        && other.ValueType == VType.Null;
            }
        }

        public double ToDouble()
        {
            return ValueType switch
            {
                VType.Float => Float,
                VType.Long => Long,
                VType.ULong => ULong,
                VType.Enum => Enum,
                _ => Double,
            };
        }

        /// <summary>
        /// Returns the value of this <see cref="UnionValue"/> as a string with a type suffix.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ToString(true);
        }

        /// <summary>
        /// Returns the value of this <see cref="UnionValue"/> as a string, 
        /// optionally with a type suffix.
        /// </summary>
        public string ToString(bool suffix, string format = null)
        {
            return ValueType switch
            {
                VType.Double => Double.ToString(format) + (suffix ? "d" : ""),
                VType.Float => Float.ToString(format) + (suffix ? "f" : ""),
                VType.Long => Long.ToString(format) + (suffix ? "l" : ""),
                VType.ULong => ULong.ToString(format) + (suffix ? "ul" : ""),
                VType.Enum => Enum.ToString(format) + (suffix ? "enum" : ""),
                VType.Null => "null",
                _ => string.Empty,
            };
        }
    }
}