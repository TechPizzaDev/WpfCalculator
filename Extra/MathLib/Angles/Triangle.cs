using MathLib.Space;

namespace MathLib.Angles
{
    [MathValue]
    public readonly struct Triangle
    {
        public double A { get; }
        public double B { get; }
        public double C { get; }

        public Triangle(double a, double b, double c)
        {
            A = a;
            B = b;
            C = c;
        }
    }
}
