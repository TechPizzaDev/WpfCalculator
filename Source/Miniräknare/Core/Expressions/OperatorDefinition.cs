using System;
using System.Linq;

namespace Miniräknare.Expressions
{
    public class OperatorDefinition
    {
        public ReadOnlyMemory<char>[] Names { get; }
        public int Precedence { get; }

        public OperatorType Type { get; }
        public OperatorSidedness Associativity { get; }

        public OperatorDefinition(
            char[] names, 
            int precedence,
            OperatorType type,
            OperatorSidedness associativity)
        {
            if (names == null) throw new ArgumentNullException(nameof(names));
            if (names.Length == 0) throw new ArgumentException(nameof(names));

            Names = names.Select(x => new ReadOnlyMemory<char>(new[] { x })).ToArray();
            Precedence = precedence;
            Type = type;
            Associativity = associativity;
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
