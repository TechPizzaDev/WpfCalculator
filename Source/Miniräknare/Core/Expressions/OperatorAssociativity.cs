using System;

namespace Miniräknare.Expressions
{
    [Flags]
    public enum OperatorAssociativity
    {
        Left = 1 << 0,
        Right = 1 << 1,
        Both = Left | Right,
        None = 1 << 2
    }
}
