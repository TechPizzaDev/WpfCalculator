using System;
using System.Linq;

namespace WpfCalculator.Expressions
{
    public class OperatorDefinition
    {
        public ReadOnlyMemory<char>[] Names { get; }
        public int Precedence { get; }

        public OperatorType Type { get; }
        public OperatorSidedness Associativity { get; }

        public OperatorDefinition(
            int precedence,
            OperatorType type,
            OperatorSidedness associativity,
            params char[] names)
        {
            if (names == null) throw new ArgumentNullException(nameof(names));
            if (names.Length == 0) throw new ArgumentException(nameof(names));

            Names = names.Select(x => new ReadOnlyMemory<char>(new[] { x })).ToArray();
            Precedence = precedence;
            Type = type;
            Associativity = associativity;
        }
    }
}
