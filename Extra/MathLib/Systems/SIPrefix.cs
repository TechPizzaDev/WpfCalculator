
namespace MathLib.Systems
{
    public readonly struct SIPrefix
    {
        private static SIPrefix[] _highSuffixes =
        {
            new SIPrefix(1/3d, "deca", "da"),
            new SIPrefix(2/3d, "hecto", "h"),
            new SIPrefix(1, "kilo", "k"),
            new SIPrefix(2, "mega", "M"),
            new SIPrefix(3, "giga", "G"),
            new SIPrefix(4, "tera", "T"),
            new SIPrefix(5, "peta", "P"),
            new SIPrefix(6, "exa", "E"),
            new SIPrefix(7, "zetta", "Z"),
            new SIPrefix(8, "yotta", "Y")
        };

        private static SIPrefix[] _lowSuffixes =
        {
            new SIPrefix(-1/3d, "deci", "d"),
            new SIPrefix(-2/3d, "centi", "c"),
            new SIPrefix(-1, "milli", "m"),
            new SIPrefix(-2, "micro", "μ"),
            new SIPrefix(-3, "nano", "n"),
            new SIPrefix(-4, "pico", "p"),
            new SIPrefix(-5, "femto", "f"),
            new SIPrefix(-6, "atto", "a"),
            new SIPrefix(-7, "zepto", "z"),
            new SIPrefix(-8, "yocto", "y")
        };

        public static readonly SIPrefix Micro = _lowSuffixes[3];

        public static readonly SIPrefix Kilo = _highSuffixes[2];

        public double Base { get; }
        public string Name { get; }
        public string Symbol { get; }

        public SIPrefix(double power, string name, string symbol)
        {
            Base = power;
            Name = name;
            Symbol = symbol;
        }
    }
}
