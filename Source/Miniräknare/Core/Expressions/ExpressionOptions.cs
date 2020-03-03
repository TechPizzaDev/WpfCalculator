using System;
using System.Collections.Generic;
using System.Linq;

namespace Miniräknare.Expressions
{
    public class ExpressionOptions
    {
        public static ExpressionOptions Default { get; } = new ExpressionOptions(
            implicitMultiplications: true,
            new[]
            {
                new OperatorDefinition('+', 0, OperatorType.Add, OperatorSidedness.OptionalLeft),
                new OperatorDefinition(new [] { '-', '–' }, 0, OperatorType.Subtract, OperatorSidedness.OptionalLeft),
                new OperatorDefinition(new [] { '*', '×' }, 1, OperatorType.Multiply, OperatorSidedness.Both),
                new OperatorDefinition(new [] { '/', ':', '÷' }, 1, OperatorType.Divide, OperatorSidedness.Both),
                new OperatorDefinition('%', 1, OperatorType.Modulo, OperatorSidedness.Both),
                new OperatorDefinition('^', 2, OperatorType.Exponent, OperatorSidedness.Both),
                new OperatorDefinition('!', 2, OperatorType.Factorial, OperatorSidedness.Left)
            });

        private OperatorDefinition[] _opDefinitions;

        public bool ImplicitMultiplications { get; }

        public ReadOnlyMemory<OperatorDefinition> OpDefinitions => _opDefinitions;

        public ExpressionOptions(
            bool implicitMultiplications,
            IEnumerable<OperatorDefinition> operatorDefinitions)
        {
            ImplicitMultiplications = implicitMultiplications;
            _opDefinitions = operatorDefinitions.ToArray();
        }

        public OperatorDefinition GetOperatorDefinition(ReadOnlySpan<char> value)
        {
            for (int i = 0; i < _opDefinitions.Length; i++)
            {
                var def = _opDefinitions[i];
                for (int j = 0; j < def.Names.Length; j++)
                {
                    var name = def.Names[j];
                    if (name.Span.SequenceEqual(value))
                        return def;
                }
            }
            return null;
        }

        public OperatorDefinition GetOperatorDefinition(ReadOnlyMemory<char> value)
        {
            return GetOperatorDefinition(value.Span);
        }

        public OperatorDefinition GetOperatorDefinition(OperatorType type)
        {
            for (int i = 0; i < _opDefinitions.Length; i++)
            {
                var def = _opDefinitions[i];
                if (def.Type == type)
                    return def;
            }
            return null;
        }
    }
}
