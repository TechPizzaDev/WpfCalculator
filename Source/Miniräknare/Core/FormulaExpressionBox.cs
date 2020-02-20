using System;
using System.Windows.Controls;

namespace Miniräknare
{
    public partial class FormulaExpressionBox : UserControl
    {
        public FormulaField ParentField { get; }

        public FormulaExpressionBox(FormulaField parentField)
        {
            InitializeComponent();

            ParentField = parentField ?? throw new ArgumentNullException(nameof(parentField));

            InputBox.ShowName = false;
            InputBox.PropertyChanged += (s, e) =>
            {
                ExpressionField.InputBox_PropertyChanged(s, e, ResultBox, StateImage);
            };
        }
    }
}
