using System;

namespace MathLib
{
    public class DerivedInternationalUnit : InternationalUnit
    {
        public string BaseUnits { get; }
        public string[] OtherBaseUnits { get; }

        public DerivedInternationalUnit(
            string name, string symbol, string[] quantity,
            string baseUnits, string[] otherBaseUnits) :
            base(name, symbol, quantity)
        {
            BaseUnits = baseUnits ?? throw new ArgumentNullException(nameof(baseUnits));
            OtherBaseUnits = otherBaseUnits ?? Array.Empty<string>();
        }
    }
}
