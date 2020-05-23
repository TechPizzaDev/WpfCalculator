using System;

namespace WpfCalculator
{
    public abstract class BoxedValue
    {
        public abstract Type Type { get; }

        public static implicit operator BoxedValue(double value)
        {
            return new BoxedValue<double>(value);
        }
    }

    public class BoxedValue<T> : BoxedValue
    {
        private T _value;

        public ref T Value => ref _value;

        public override Type Type => typeof(T);

        public BoxedValue(T value = default)
        {
            Value = value;
        }
    }
}
