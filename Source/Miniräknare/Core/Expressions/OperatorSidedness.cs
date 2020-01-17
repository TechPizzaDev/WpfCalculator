using System;

namespace Miniräknare.Expressions
{
    [Flags]
    public enum OperatorSidedness
    {
        Left, // TODO
        Right,
        Both,
        OptionalLeft,
        OptionalRight, // TODO
    }
}
