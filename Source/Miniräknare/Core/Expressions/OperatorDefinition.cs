using System;
using System.Linq;

namespace Miniräknare.Expressions
{
    public class OperatorDefinition
    {
        public ReadOnlyMemory<char>[] Names { get; }
        public int Precedence { get; }

        public OperatorType Type { get; }
        public OperatorAssociativity Associativity { get; }

        public OperatorDefinition(
            char[] names, 
            int precedence,
            OperatorType type,
            OperatorAssociativity associativity)
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
            OperatorAssociativity sidedness) :
            this(new[] { name }, priority, type, sidedness)
        {
        }
    }
}
