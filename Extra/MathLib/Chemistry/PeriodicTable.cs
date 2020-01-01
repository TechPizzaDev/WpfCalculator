using System.Collections.Generic;

namespace MathLib.Chemistry
{
    public class PeriodicTable
    {
        public enum MatterType
        {
            Metal,
            Metalliod,
            NonMetal
        }

        public enum TrivialName
        {
            AlkaliMetal = 1,
            AlkalineEarthMetal = 2,
            Coin­ageMetal = 11,
            Triels = 13,
            Tetrels = 14,
            Pnictogens = 15,
            Chalcogens = 16,
            Halogens = 17,
            NobleGas = 18
        }

        public enum GroupName
        {
            Lithium,
            Beryllium,
            Scandium,
            Titanium,
            Vanadium,
            Chromium,
            Manganese,
            Iron,
            Cobalt,
            Nickel,
            Copper,
            Zinc,
            Boron,
            Carbon,
            Nitrogen,
            Oxygen,
            Fluorine,
            Neon
        }

        public readonly struct Element
        {
            public int Number { get; }
            public string Name { get; }
            public string ShortName { get; }

            public MatterType Type { get; }
            public TrivialName TrivialName { get; }
            public GroupName GroupName { get; }
        }

        private static Dictionary<int, Element> _elements;
    }
}
