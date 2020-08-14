using System;

namespace WpfCalculator.Expressions
{
    public class OperatorInverse
    {
        public OperatorDefinition SourceDefinition { get; }
        public OperatorDefinition Definition { get; }

        public OperatorInverse(
            OperatorDefinition sourceDefinition, OperatorDefinition definition)
        {
            SourceDefinition = sourceDefinition ?? throw new ArgumentNullException(nameof(sourceDefinition));
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }
    }
}
