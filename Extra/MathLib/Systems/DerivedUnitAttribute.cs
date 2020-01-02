using System;

namespace MathLib.Systems
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
    public class DerivedUnitAttribute : MathValueAttribute
    {
        // TODO: add coherent derived unit detection somewhere
    }
}
