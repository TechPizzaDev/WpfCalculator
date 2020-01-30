using System;
using System.Runtime.InteropServices;

namespace Miniräknare
{
    public enum UnionValueType : int
    {
        Null = 0,
        Float,
        Double,
        Long,
        ULong,
        Enum
    }

    [StructLayout(LayoutKind.Explicit)]
    public readonly struct UnionValue : IEquatable<UnionValue>
    {
        public static UnionValue Null { get; } = new UnionValue(UnionValueType.Null);

        [field: FieldOffset(0)] public UnionValueType Type { get; }
        [field: FieldOffset(sizeof(UnionValueType))] public float Float { get; }
        [field: FieldOffset(sizeof(UnionValueType))] public double Double { get; }
        [field: FieldOffset(sizeof(UnionValueType))] public long Long { get; }
        [field: FieldOffset(sizeof(UnionValueType))] public ulong ULong { get; }
        public ulong Enum => ULong;

        private UnionValue(UnionValueType type) : this() => Type = type;
        private UnionValue(UnionValueType type, ulong value) : this(type) => ULong = value;

        public UnionValue(double value) : this(UnionValueType.Double) => Double = value;
        public UnionValue(float value) : this(UnionValueType.Float) => Float = value;
        public UnionValue(long value) : this(UnionValueType.Long) => Long = value;

        public static implicit operator UnionValue(double value) => new UnionValue(value);
        public static implicit operator UnionValue(float value) => new UnionValue(value);

        public static UnionValue FromEnum<TEnum>(TEnum value)
            where TEnum : Enum, IConvertible
        {
            return new UnionValue(UnionValueType.Enum, value.ToUInt64(null));
        }

        public bool Equals(UnionValue other)
        {
            switch (Type & other.Type)
            {
                case UnionValueType.Double: return Double == other.Double;
                case UnionValueType.Float: return Float == other.Float;
                case UnionValueType.Long: return Long == other.Long;

                case UnionValueType.Enum:
                case UnionValueType.ULong: return ULong == other.ULong;

                case UnionValueType.Null:
                default:
                    return Type == UnionValueType.Null
                        && other.Type == UnionValueType.Null;
            }
        }

        public override string ToString()
        {
            return ToString(true);
        }

        public string ToString(bool suffix)
        {
            return Type switch
            {
                UnionValueType.Double => Double.ToString() + (suffix ? "d" : ""),
                UnionValueType.Float => Float.ToString() + (suffix ? "f" : ""),
                UnionValueType.Long => Long.ToString() + (suffix ? "l" : ""),
                UnionValueType.ULong => ULong.ToString() + (suffix ? "ul" : ""),
                UnionValueType.Enum => Enum.ToString() + (suffix ? "e" : ""),
                UnionValueType.Null => "null",
                _ => string.Empty,
            };
        }
    }
}