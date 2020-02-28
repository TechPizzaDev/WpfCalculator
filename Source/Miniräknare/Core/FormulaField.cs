using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Miniräknare
{
    public partial class FormulaField : UserControl
    {
        private List<FormulaExpressionBox> _formulaExpressions;

        public FormulaField()
        {
            InitializeComponent();

            _formulaExpressions = new List<FormulaExpressionBox>();
            FormulaList.ItemsSource = _formulaExpressions;

            int count = 3;
            for (int i = 0; i < count; i++)
            {
                var formula = new FormulaExpressionBox(this);

                var disabledDirection = i == 0
                    ? FocusNavigationDirection.Previous
                    : (i == count - 1)
                    ? FocusNavigationDirection.Next
                    : (FocusNavigationDirection?)null;

                formula.InputBox.PreviewKeyDown += (s, e) => TextValue_KeyDown(e, disabledDirection);

                _formulaExpressions.Add(formula);
            }
        }

        private static void TextValue_KeyDown(KeyEventArgs e, FocusNavigationDirection? disabledDirection)
        {
            TraversalRequest request = null;

            if (e.Key == Key.Up && disabledDirection != FocusNavigationDirection.Previous)
                request = new TraversalRequest(FocusNavigationDirection.Previous);
            else if (e.Key == Key.Down && disabledDirection != FocusNavigationDirection.Next)
                request = new TraversalRequest(FocusNavigationDirection.Next);
            
            if (request != null)
                ((TextBox)e.OriginalSource).MoveFocus(request);
        }

        private void OpenContextMenu(object sender, RoutedEventArgs e)
        {
            MainGrid.ContextMenu.IsOpen = true;
        }
    }
}
