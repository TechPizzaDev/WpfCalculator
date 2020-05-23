using System;

namespace WpfCalculator.Expressions
{
    [Flags]
    public enum OperatorSidedness
    {
        Left = 1 << 0,
        Right = 1 << 1,
        Both = Left | Right,

        OptionalLeft = Right | 1 << 2,
        OptionalRight = Left | 1 << 3
    }
}
