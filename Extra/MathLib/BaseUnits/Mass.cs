using MathLib.Systems;

namespace MathLib.Forces
{
    [MathValue]
    public class Mass : BaseUnit
    {
        public override double Value => Kilo;

        public double Grams => Kilo / 1000;
        public double Kilo { get; }

        public Mass(double kiloGrams) => Kilo = kiloGrams;
    }
}
