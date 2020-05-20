using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Miniräknare
{
    public partial class EquationField : UserControl, IContextMenuOperand, IHasContextMenu
    {
        private List<EquationExpressionBox> _equationExpressionBoxes;

        public EquationField()
        {
            InitializeComponent();

            _equationExpressionBoxes = new List<EquationExpressionBox>();
            EquationList.ItemsSource = _equationExpressionBoxes;

            int count = 3;
            for (int i = 0; i < count; i++)
            {
                var equation = new EquationExpressionBox(this);

                int currentIndex = i;
                equation.InputBox.PreviewKeyDown += (s, e) => TextValue_KeyDown(e, currentIndex);

                _equationExpressionBoxes.Add(equation);
            }
        }

        private void TextValue_KeyDown(KeyEventArgs e, int currentIndex)
        {
            EquationExpressionBox box = null;
            if (e.Key == Key.Up && currentIndex > 0)
                box = EquationList.Items[currentIndex - 1] as EquationExpressionBox;
            else if (e.Key == Key.Down && currentIndex < EquationList.Items.Count - 1)
                box = EquationList.Items[currentIndex + 1] as EquationExpressionBox;

            box?.InputBox.ValueBox.Focus();
        }

        private void OpenContextMenu(object sender, RoutedEventArgs e)
        {
            OpenContextMenu();
        }

        public void OpenContextMenu()
        {
            ContextMenu.PlacementTarget = this;
            ContextMenu.IsOpen = true;
        }

        public void RegisterContextMenuItem(MenuItem menuItem)
        {
            menuItem.Click += ContextMenuItem_Click;
        }

        public void UnregisterContextMenuItem(MenuItem menuItem)
        {
            menuItem.Click -= ContextMenuItem_Click;
        }

        private void ContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuItem)sender;
            switch (menuItem.Name)
            {
                case "Remove":
                    Console.WriteLine("equations" + ": " + "Remove");
                    break;
            }
        }
    }
}
