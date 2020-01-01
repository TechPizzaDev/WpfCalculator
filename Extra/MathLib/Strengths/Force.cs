
using System;

namespace MathLib.Strengths
{
    public readonly struct Force
    {
        public const string Unit = "Newton";

        public double Newtons { get; }

        public Force(double newtons) => Newtons = newtons;

        public static implicit operator double(Force force) => force.Newtons;

        public static Force FromNewtons(double newtons) => new Force(newtons);
        public static Force FromKiloNewtons(double kiloNewtons) => new Force(kiloNewtons * 1000);
        public static Force FromNewtonsPow(double value, double exponent) => new Force(Math.Pow(value, exponent));

        public override string ToString()
        {
            return System.Math.Round(Newtons, 2) + " " + Unit;
        }
    }
}
