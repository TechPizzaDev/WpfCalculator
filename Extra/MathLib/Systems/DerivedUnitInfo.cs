using System;

namespace MathLib
{
    public class DerivedUnitInfo : UnitInfo
    {
        public UnitInfo[] BaseUnits { get; }
        public UnitInfo[] OtherBaseUnits { get; }

        public DerivedUnitInfo(
            string name, string symbol, string[] quantity,
            UnitInfo[] baseUnits, 
            UnitInfo[] otherBaseUnits) :
            base(name, symbol, quantity)
        {
            BaseUnits = baseUnits ?? throw new ArgumentNullException(nameof(baseUnits));
            OtherBaseUnits = otherBaseUnits ?? Array.Empty<UnitInfo>();
        }
    }
}
