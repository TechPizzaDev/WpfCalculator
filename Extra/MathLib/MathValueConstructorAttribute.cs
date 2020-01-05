using System;

namespace MathLib
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class MathValueConstructorAttribute : Attribute
    {
        public MathValueConstructorAttribute()
        {
        }
    }
}
