using System;
using System.Linq;

namespace Miniräknare.Expressions
{
    public class OperatorDefinition
    {
        public ReadOnlyMemory<char>[] Names { get; }
        public int Priority { get; }

        public OperatorType Type { get; }
        public OperatorSidedness Sidedness { get; }

        public OperatorDefinition(
            char[] names, 
            int priority,
            OperatorType type,
            OperatorSidedness sidedness)
        {
            if (names == null) throw new ArgumentNullException(nameof(names));
            if (names.Length == 0) throw new ArgumentException(nameof(names));

            Names = names.Select(x => new ReadOnlyMemory<char>(new[] { x })).ToArray();
            Priority = priority;
            Type = type;
            Sidedness = sidedness;
        }

        public OperatorDefinition(
            char name,
            int priority,
            OperatorType type,
            OperatorSidedness sidedness) :
            this(new[] { name }, priority, type, sidedness)
        {
        }
    }
}
