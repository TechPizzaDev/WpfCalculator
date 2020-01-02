using System;
using MathLib.Systems;

namespace MathLib.Space
{
    [Serializable]
    [MathValue]
    public readonly struct Length : IBaseUnit
    {
        double IBaseUnit.Value => Meters;

        public double Meters { get; }
        public double Milli => Meters * (1_000);
        public double Micro => Meters * (1000_000_000);

        public Length(double meters) => Meters = meters;

        #region From

        public static Length FromMeters(double meters) =>
            new Length(meters);

        public static Length FromMilli(double milliMeters) =>
            new Length(milliMeters / (1000));

        public static Length FromMicro(double milliMeters) =>
            new Length(milliMeters / (1000_000_000));

        #endregion

        public static implicit operator double(Length length) => length.Meters;
        public static implicit operator Length(double meters) => new Length(meters);

        #region Math Operators

        public static Length operator +(Length a, Length b) =>
            FromMeters(a.Meters + b.Meters);

        public static Length operator -(Length a, Length b) =>
            FromMeters(a.Meters - b.Meters);

        public static Length operator *(Length left, Length right) =>
            FromMeters(left.Meters * right.Meters);

        public static Length operator /(Length left, Length right) =>
            FromMeters(left.Meters / right.Meters);

        public static Length operator /(Area left, Length right) =>
            FromMeters(left.Meters / right.Meters);

        #endregion

        public override string ToString()
        {
            if (Meters < 0.1)
                return Math.Round(Milli, 3) + " millimeters";
            return Meters + " meters";
        }
    }
}
