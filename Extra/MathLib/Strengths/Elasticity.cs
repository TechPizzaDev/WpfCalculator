using MathLib.Space;

namespace MathLib.Strengths
{
    public readonly struct Elasticity
    {
        public const string Unit = "N/mm^2";

        public double Drag { get; }
        
        public double Tension { get; }

        public static double FromTension(Force force, Area area, Tension tension) =>
            (force.Newtons * tension.Length) / (area.Meters * tension.Extension);
    }
}
