using System;

namespace MathLib.Space
{
    public readonly struct Area
    {
        public double Meters { get; }
        public double MilliMeters => Meters * (1000 * 1000);

        public Area(double meters)
        {
            Meters = meters;
        }

        #region From

        public static Area FromMeters(double meters) =>
            new Area(meters);

        public static Area FromMilliMeters(double milliMeters) => 
            new Area(milliMeters / (1000 * 1000));

        #endregion

        public static implicit operator double(Area area) => area.Meters;
        public static implicit operator Area(double meters) => new Area(meters);

        public static explicit operator Area(Length length) => new Area(length / 1000);

        #region Math Operators

        public static Area operator +(Area a, Area b) => 
            FromMeters(a.Meters + b.Meters);

        public static Area operator -(Area left, Area right) =>
            FromMeters(left.Meters + right.Meters);

        public static Area operator *(Area left, float factor) =>
            FromMeters(left.Meters * factor);

        public static Area operator /(Area left, float divider) =>
            FromMeters(left.Meters / divider);

        #endregion

        public override string ToString()
        {
            double log = Math.Log(Meters, 1000);
            if(log > 0)
            {

            }
            else
            {

            }

            if (Meters < 0.1)
                return MilliMeters + " millimeters";
            return Meters + " meters";
        }
    }
}
