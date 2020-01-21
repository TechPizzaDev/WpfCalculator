using System;

namespace Miniräknare.Expressions
{
    public class OperatorDefinition
    {
        public ReadOnlyMemory<char> Name { get; }
        public int Priority { get; }

        public OperatorType Type { get; }
        public OperatorSidedness Sidedness { get; }

        public OperatorDefinition(
            ReadOnlyMemory<char> name, 
            int priority,
            OperatorType type,
            OperatorSidedness sidedness)
        {
            if (name.IsEmpty)
                throw new ArgumentException(nameof(name));

            Name = name;
            Priority = priority;
            Type = type;
            Sidedness = sidedness;
        }
    }
}
