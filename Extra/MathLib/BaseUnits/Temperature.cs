using MathLib.Systems;

namespace MathLib
{
    [MathValue]
    public class Temperature : BaseUnit
    {
        public override double Value => Kelvin;

        public double Kelvin { get; }
        public double Celcius => Kelvin - 273.15;

        public Temperature(double kelvin) => Kelvin = kelvin;
    }
}
