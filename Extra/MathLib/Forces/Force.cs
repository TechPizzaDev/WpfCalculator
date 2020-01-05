using System;
using System.Xml.Serialization;

namespace MathLib.Forces
{
    [Serializable]
    [MathValue]
    public class Force
    {
        public const string Unit = "Newton";

        [XmlText]
        public double Newtons { get; protected set; }

        protected Force()
        {
        }

        public Force(double newtons) => Newtons = newtons;

        public static implicit operator double(Force force) => force.Newtons;

        [MathValueConstructor]
        public static Force FromNewtons(double newtons) => new Force(newtons);

        public static Force FromKiloNewtons(double kiloNewtons) => new Force(kiloNewtons * 1000);
        public static Force FromNewtonsPow(double value, double exponent) => new Force(Math.Pow(value, exponent));

        public override string ToString()
        {
            return Math.Round(Newtons, 2) + " " + Unit;
        }
    }
}
