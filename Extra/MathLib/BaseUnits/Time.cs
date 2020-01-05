using System;
using MathLib.Systems;

namespace MathLib
{
    [MathValue]
    public class Time : BaseUnit
    {
        private TimeSpan _value;

        public override double Value => Seconds;

        public double Seconds => _value.TotalSeconds;

        public Time(TimeSpan value) => _value = value;
    }
}
