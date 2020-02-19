using System;

namespace Miniräknare
{
    public class FormulaExpression
    {
        public FormulaField Parent { get; }
        public ExpressionBox Expression { get; set; }

        public bool IsTargeted { get; set; }

        public FormulaExpression(FormulaField parent)
        {
            Parent = parent ?? throw new ArgumentNullException(nameof(parent));
        }
    }
}
