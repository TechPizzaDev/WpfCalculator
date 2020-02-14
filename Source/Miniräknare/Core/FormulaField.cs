using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Miniräknare.Expressions;

namespace Miniräknare
{
    public partial class FormulaField
    {
        public BindingList<string> Fields { get; }

        public FormulaField()
        {
        }

        public FormulaField(ExpressionOptions options) : this()
        {
            Fields = new BindingList<string>();

            for (int i = 0; i < 10; i++)
            {
                //var box = new ExpressionBox(options);
                //box.Loaded += Box_Loaded;

                //box.ShowName = false;

                Fields.Add(i.ToString());
            }
        }

        private void List_Loaded(object sender, RoutedEventArgs e)
        {
            var list = (ItemsControl)sender;
            var itemPresenter = (FrameworkElement)VisualTreeHelper.GetChild(list, 0);
            var stackPanel = (FrameworkElement)VisualTreeHelper.GetChild(itemPresenter, 0);

            int count = VisualTreeHelper.GetChildrenCount(stackPanel);
            for (int i = 0; i < count; i++)
            {
                var contentPresenter = (FrameworkElement)VisualTreeHelper.GetChild(stackPanel, i);
                var grid = (FrameworkElement)VisualTreeHelper.GetChild(contentPresenter, 0);

                var valueBox = (ExpressionBox)grid.FindName("ValueBox");
                var disabledDirection = i == 0 
                    ? FocusNavigationDirection.Previous 
                    : (i == count - 1) 
                    ? FocusNavigationDirection.Next 
                    : (FocusNavigationDirection?)null;
                valueBox.PreviewKeyDown += (s, e) => TextValue_KeyDown(e, disabledDirection);
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
    }
}
