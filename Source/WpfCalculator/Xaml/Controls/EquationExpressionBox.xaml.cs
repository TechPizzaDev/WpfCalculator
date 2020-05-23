using System;
using System.Windows.Controls;

namespace WpfCalculator
{
    public partial class EquationExpressionBox : UserControl
    {
        public EquationField ParentField { get; }

        public EquationExpressionBox(EquationField parentField)
        {
            ParentField = parentField ?? throw new ArgumentNullException(nameof(parentField));

            InitializeComponent();

            InputBox.ShowName = false;
            InputBox.PropertyChanged += (s, e) =>
            {
                ExpressionField.InputBox_PropertyChanged(s, e, ResultBox, StateImage);
            };
        }
    }
}
