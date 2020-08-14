using System;
using System.Collections.Generic;
using System.Linq;

namespace WpfCalculator.Expressions
{
    public class ExpressionOptions
    {
        public static ExpressionOptions Default { get; }

        private OperatorDefinition[] _opDefinitions;
        private OperatorInverse[] _opInverses;

        public bool DoImplicitMultiplications { get; }

        public ReadOnlyMemory<OperatorDefinition> OpDefinitions => _opDefinitions;

        static ExpressionOptions()
        {
            var opDefs = new Dictionary<OperatorType, OperatorDefinition>();
            void AddOpDef(int precedence, OperatorType type, OperatorSidedness sidedness, params char[] names)
            {
                var opDef = new OperatorDefinition(precedence, type, sidedness, names);
                opDefs.Add(type, opDef);
            }

            AddOpDef(0, OperatorType.Add, OperatorSidedness.OptionalLeft, '+');
            AddOpDef(0, OperatorType.Subtract, OperatorSidedness.OptionalLeft, '-', '–');
            AddOpDef(1, OperatorType.Multiply, OperatorSidedness.Both, '*', '×');
            AddOpDef(1, OperatorType.Divide, OperatorSidedness.Both, '/', ':', '÷');
            AddOpDef(1, OperatorType.Modulo, OperatorSidedness.Both, '%');
            AddOpDef(2, OperatorType.Exponent, OperatorSidedness.Both, '^');
            AddOpDef(2, OperatorType.Factorial, OperatorSidedness.Left, '!');

            var opInverses = new List<OperatorInverse>();
            void AddOpInverse(OperatorType source, OperatorType inverse)
            {
                var sourceOp = opDefs[source];
                var inverseOp = opDefs[inverse];
                opInverses.Add(new OperatorInverse(sourceOp, inverseOp));
            }

            AddOpInverse(OperatorType.Add, OperatorType.Subtract);
            AddOpInverse(OperatorType.Subtract, OperatorType.Add);
            AddOpInverse(OperatorType.Multiply, OperatorType.Divide);
            AddOpInverse(OperatorType.Divide, OperatorType.Multiply);

            Default = new ExpressionOptions(
                opDefs.Values,
                opInverses,
                doImplicitMultiplications: true);
        }

        public ExpressionOptions(
            IEnumerable<OperatorDefinition> operatorDefinitions,
            IEnumerable<OperatorInverse> operatorInverses,
            bool doImplicitMultiplications)
        {
            if (operatorDefinitions == null)
                throw new ArgumentNullException(nameof(operatorDefinitions));
            if (operatorInverses == null)
                throw new ArgumentNullException(nameof(operatorInverses));

            _opDefinitions = operatorDefinitions.ToArray();
            _opInverses = operatorInverses.ToArray();

            DoImplicitMultiplications = doImplicitMultiplications;
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
            return _opDefinitions.FirstOrDefault(x => x.Type == type);
        }

        public OperatorInverse GetOperatorInverse(OperatorType sourceType)
        {
            return _opInverses.FirstOrDefault(x => x.SourceDefinition.Type == sourceType);
        }
    }
}
