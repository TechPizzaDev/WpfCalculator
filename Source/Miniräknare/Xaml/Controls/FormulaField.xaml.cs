using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Miniräknare
{
    public partial class FormulaField : UserControl, IHasContextMenu
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

                int currentIndex = i;
                formula.InputBox.PreviewKeyDown += (s, e) => TextValue_KeyDown(e, currentIndex);

                _formulaExpressions.Add(formula);
            }
        }

        private void TextValue_KeyDown(KeyEventArgs e, int currentIndex)
        {
            FormulaExpressionBox box = null;
            if (e.Key == Key.Up && currentIndex > 0)
                box = FormulaList.Items[currentIndex - 1] as FormulaExpressionBox;
            else if (e.Key == Key.Down && currentIndex < FormulaList.Items.Count - 1)
                box = FormulaList.Items[currentIndex + 1] as FormulaExpressionBox;

            box?.InputBox.ValueBox.Focus();
        }

        private void OpenContextMenu(object sender, RoutedEventArgs e)
        {
            OpenContextMenu();
        }

        public void OpenContextMenu()
        {
            ContextMenu.IsOpen = true;
        }

        public void RemoveField()
        {

        }
    }
}
