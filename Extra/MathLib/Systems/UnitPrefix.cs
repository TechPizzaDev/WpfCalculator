
namespace MathLib.Systems
{
    public readonly struct UnitPrefix
    {
        private static UnitPrefix[] _highSuffixes =
        {
            new UnitPrefix(1/3d, "deca", "da"),
            new UnitPrefix(2/3d, "hecto", "h"),
            new UnitPrefix(1, "kilo", "k"),
            new UnitPrefix(2, "mega", "M"),
            new UnitPrefix(3, "giga", "G"),
            new UnitPrefix(4, "tera", "T"),
            new UnitPrefix(5, "peta", "P"),
            new UnitPrefix(6, "exa", "E"),
            new UnitPrefix(7, "zetta", "Z"),
            new UnitPrefix(8, "yotta", "Y")
        };

        private static UnitPrefix[] _lowSuffixes =
        {
            new UnitPrefix(-1/3d, "deci", "d"),
            new UnitPrefix(-2/3d, "centi", "c"),
            new UnitPrefix(-1, "milli", "m"),
            new UnitPrefix(-2, "micro", "μ"),
            new UnitPrefix(-3, "nano", "n"),
            new UnitPrefix(-4, "pico", "p"),
            new UnitPrefix(-5, "femto", "f"),
            new UnitPrefix(-6, "atto", "a"),
            new UnitPrefix(-7, "zepto", "z"),
            new UnitPrefix(-8, "yocto", "y")
        };

        public static readonly UnitPrefix Micro = _lowSuffixes[3];

        public static readonly UnitPrefix Kilo = _highSuffixes[2];

        public double Base { get; }
        public string Name { get; }
        public string Symbol { get; }

        public UnitPrefix(double power, string name, string symbol)
        {
            Base = power;
            Name = name;
            Symbol = symbol;
        }
    }
}
