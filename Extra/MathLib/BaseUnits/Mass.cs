using MathLib.Systems;

namespace MathLib.BaseUnits
{
    [MathValue]
    public readonly struct Mass : IBaseUnit
    {
        double IBaseUnit.Value => Kilo;

        public double Grams => Kilo / 1000;
        public double Kilo { get; }

        public Mass(double kiloGrams) => Kilo = kiloGrams;
    }
}
