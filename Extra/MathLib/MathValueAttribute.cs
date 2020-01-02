using System;

namespace MathLib
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
    public class MathValueAttribute : Attribute
    {
        public MathValueAttribute()
        {
        }
    }
}
