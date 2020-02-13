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
            Console.WriteLine();
            var list = (ItemsControl)sender;
            var a = (FrameworkElement)VisualTreeHelper.GetChild(list, 0);
            var b = (FrameworkElement)VisualTreeHelper.GetChild(a, 0);
            var c = (FrameworkElement)VisualTreeHelper.GetChild(b, 0);
            var d = (FrameworkElement)VisualTreeHelper.GetChild(c, 0);

            var valueBox = (ExpressionBox)d.FindName("ValueBox");
            valueBox.PreviewKeyDown += TextValue_KeyDown;
        }

        private void Box_Loaded(object sender, RoutedEventArgs e)
        {
            var element = (FrameworkElement)sender;
            var textValueBox = (TextBox)element.FindName("ValueBox");
            textValueBox.PreviewKeyDown += TextValue_KeyDown;
            Console.WriteLine("what"); 
        }

        private void TextValue_KeyDown(object sender, KeyEventArgs e)
        {
            var box = (ExpressionBox)sender;

            if (e.Key == Key.Up)
            {

            }
            else if (e.Key == Key.Down)
            {

            }
            Console.WriteLine(sender + ", " + e);
        }
    }
}
