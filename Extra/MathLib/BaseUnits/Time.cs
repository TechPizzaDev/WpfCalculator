using System;
using MathLib.Systems;

namespace MathLib.BaseUnits
{
    [MathValue]
    public readonly struct Time : IBaseUnit
    {
        double IBaseUnit.Value => Value.TotalSeconds;

        public TimeSpan Value { get; }

        public double Seconds => Value.TotalSeconds;

        public Time(TimeSpan value) => Value = value;
    }
}
