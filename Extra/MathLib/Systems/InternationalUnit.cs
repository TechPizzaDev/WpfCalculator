using System;

namespace MathLib
{
    public class InternationalUnit
    {
        public string Name { get; }
        public string Symbol { get; }
        public string[] Quantity { get; }

        public InternationalUnit(string name, string symbol, string[] quantity)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            Quantity = quantity ?? throw new ArgumentNullException(nameof(quantity));

            if (Quantity.Length == 0)
                throw new ArgumentException("May not be empty.", nameof(quantity));
        }
    }
}
