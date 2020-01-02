using MathLib.Systems;

namespace MathLib.BaseUnits
{
    [MathValue]
    public readonly struct Temperature : IBaseUnit
    {
        double IBaseUnit.Value => Kelvin;

        public double Kelvin { get; }
        public double Celcius => Kelvin - 273.15;

        public Temperature(double kelvin) => Kelvin = kelvin;
    }
}
