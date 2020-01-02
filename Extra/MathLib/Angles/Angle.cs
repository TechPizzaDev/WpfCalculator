using System;

namespace MathLib.Angles
{
    [MathValue]
    public readonly struct Angle
    {
        public static double FromPythagoras(double a, double b)
        {
            double cPow = Math.Pow(a, 2) + Math.Pow(b, 2);
            return Math.Sqrt(cPow);
        }
    }
}
