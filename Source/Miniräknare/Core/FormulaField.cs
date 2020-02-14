﻿using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Miniräknare.Expressions;

namespace Miniräknare
{
    public class FormulaExpression
    {
        public FormulaField Parent { get; }
        public ExpressionBox Expression { get; set; }

        public FormulaExpression(FormulaField parent)
        {
            Parent = parent ?? throw new ArgumentNullException(nameof(parent));
        }
    }

    public partial class FormulaField
    {
        public BindingList<FormulaExpression> Expressions { get; }

        public FormulaField()
        {
        }

        public FormulaField(ExpressionOptions options) : this()
        {
            Expressions = new BindingList<FormulaExpression>();

            for (int i = 0; i < 3; i++)
            {
                //var box = new ExpressionBox(options);
                //box.Loaded += Box_Loaded;

                //box.ShowName = false;

                Expressions.Add(new FormulaExpression(this));
            }
        }

        private void List_Loaded(object sender, RoutedEventArgs e)
        {
            var list = (ItemsControl)sender;
            var itemPresenter = (FrameworkElement)VisualTreeHelper.GetChild(list, 0);
            var stackPanel = (FrameworkElement)VisualTreeHelper.GetChild(itemPresenter, 0);

            var fields = (BindingList<FormulaExpression>)list.ItemsSource;

            int count = VisualTreeHelper.GetChildrenCount(stackPanel);
            for (int i = 0; i < count; i++)
            {
                var contentPresenter = (FrameworkElement)VisualTreeHelper.GetChild(stackPanel, i);
                var grid = (FrameworkElement)VisualTreeHelper.GetChild(contentPresenter, 0);

                var valueBox = (ExpressionBox)grid.FindName("ValueBox");
                fields[i].Expression = valueBox;

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
