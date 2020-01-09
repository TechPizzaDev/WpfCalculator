using System;

namespace Miniräknare.Expressions
{
    public class OperatorDefinition
    {
        public ReadOnlyMemory<char> Name { get; }
        public bool RequiresBothSides { get; }
        public int Priority { get; }

        public OperatorDefinition(ReadOnlyMemory<char> name, bool requiresBothSides, int priority)
        {
            if (name.IsEmpty)
                throw new ArgumentException(nameof(name));

            Name = name;
            RequiresBothSides = requiresBothSides;
            Priority = priority;
        }

        public static bool GetRequiresBothSides(OperatorDefinition definition)
        {
            return definition == null || definition.RequiresBothSides;
        }
    }
}
