using System;

namespace WpfCalculator
{
    [Flags]
    public enum NumberGroup
    {
        Natural = 1 << 0,
        Whole = Natural | 1 << 1,
        Integers = Whole | 1 << 2,
        Rational = Integers | 1 << 3,

        Irrational = 1 << 4,
        Real = Rational | Irrational,
        Imaginary = 1 << 5
    }
}