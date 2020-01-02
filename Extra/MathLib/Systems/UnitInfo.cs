using System;

namespace MathLib
{
    public class UnitInfo
    {
        public string Name { get; }
        public string Symbol { get; }
        public string[] Quantities { get; }

        public UnitInfo(string name, string symbol, string[] quantities)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            Quantities = quantities ?? throw new ArgumentNullException(nameof(quantities));

            if (Quantities.Length == 0)
                throw new ArgumentException("May not be empty.", nameof(quantities));
        }

        // TODO: add static functions that describe different units and types
    }
}
