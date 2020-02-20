using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Miniräknare.Expressions;

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
        }

        public FormulaField(ExpressionOptions options) : this()
        {
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

        //private void List_Loaded(object sender, RoutedEventArgs e)
        //{
        //    var list = (ItemsControl)sender;
        //    var itemPresenter = (FrameworkElement)VisualTreeHelper.GetChild(list, 0);
        //    var stackPanel = (FrameworkElement)VisualTreeHelper.GetChild(itemPresenter, 0);
        //
        //    var fields = (BindingList<FormulaExpression>)list.ItemsSource;
        //
        //    int count = VisualTreeHelper.GetChildrenCount(stackPanel);
        //    for (int i = 0; i < count; i++)
        //    {
        //        var contentPresenter = (FrameworkElement)VisualTreeHelper.GetChild(stackPanel, i);
        //        var grid = (FrameworkElement)VisualTreeHelper.GetChild(contentPresenter, 0);
        //
        //        var input = (ExpressionBox)grid.FindName("InputBox");
        //        fields[i].Input = input;
        //
        //        InitializeInputBox(input);
        //    }
        //}

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
    }
}
