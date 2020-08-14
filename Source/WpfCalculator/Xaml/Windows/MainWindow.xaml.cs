using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Effects;
using WpfCalculator.Expressions;

namespace WpfCalculator
{
    // TODO: settings:
    //  * smooth scroll in field list (quality vs performance)
    //  * show result values with base10 exponent (default) or just a lot of zeroes (+ max decimal count)
    //  * result value with either comma or dot

    public partial class MainWindow : Window
    {
        private BindingList<ListViewItem> FieldItemList { get; } =
            new BindingList<ListViewItem>();

        private LanguageWindow? _languageWindow;
        private AppStateProvider _stateProvider;

        public AppState State => _stateProvider.State;

        public MainWindow()
        {
            InitializeComponent();

            _stateProvider = App.Instance.StateProvider;

            FieldListView.ItemsSource = FieldItemList;

            AddNewEquationField(State, null);
            AddNewField(State);

            var menuFieldOptions = (ContextMenu)FindResource("Menu_FieldOptions");
            menuFieldOptions.Opened += MenuFieldOptions_Opened;
            menuFieldOptions.Closed += MenuFieldOptions_Closed;
        }

        private IContextMenuOperand? TryGetFieldFromContextMenu(ContextMenu menu)
        {
            var target = menu.PlacementTarget;
            if (menu.PlacementTarget is ListViewItem listItem)
                target = listItem.Content as UIElement;

            return target as IContextMenuOperand;
        }

        private void MenuFieldOptions_Opened(object sender, RoutedEventArgs e)
        {
            var menu = (ContextMenu)sender;
            var field = TryGetFieldFromContextMenu(menu);
            if (field != null)
            {
                foreach (var menuItem in menu.Items.OfType<MenuItem>())
                    field.RegisterContextMenuItem(menuItem);
            }
        }

        private void MenuFieldOptions_Closed(object sender, RoutedEventArgs e)
        {
            var menu = (ContextMenu)sender;
            var field = TryGetFieldFromContextMenu(menu);
            if (field != null)
            {
                foreach (var menuItem in menu.Items.OfType<MenuItem>())
                    field.UnregisterContextMenuItem(menuItem);
            }
        }

        #region ActionButton Click handlers

        private void AddNewEquationField(AppState state, EquationSet equation)
        {
            var field = new EquationField();
            FieldItemList.Add(new ListViewItem() { Content = field, ContextMenu = field.ContextMenu });
        }

        private void AddNewField(AppState state)
        {
            var field = new ExpressionField();
            field.InputBox.VariableName = state.GenerateFieldName().ToString();

            var listItem = new ListViewItem
            {
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Content = field,
                ContextMenu = field.ContextMenu
            };

            FieldItemList.Add(listItem);
        }

        private void AddNewField_Click(object sender, RoutedEventArgs e)
        {
            AddNewField(State);
        }

        private void AddNewFunctionField_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button))
                return;

            button.ContextMenu.IsOpen = true;
        }

        #endregion

        private void FieldList_MouseDown(object? sender, MouseButtonEventArgs e)
        {
            if (!(sender is ListView list))
                return;

            list.UnselectAll();
        }

        #region MenuItem Click handlers

        private void MenuItem_Exit_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MenuItem_Language_Click(object? sender, RoutedEventArgs e)
        {
            if (_languageWindow == null)
            {
                _languageWindow = new LanguageWindow();
                _languageWindow.Closed += LanguageWindow_Closed;
            }

            _languageWindow.Show();
            _languageWindow.Focus();
        }

        private void LanguageWindow_Closed(object? sender, EventArgs e)
        {
            _languageWindow = null;
        }

        #endregion

        private void Window_Closing(object? sender, CancelEventArgs e)
        {

            // TODO: check if there is stuff to save
        }

        private void MenuItem_View_Theme_Click(object sender, RoutedEventArgs e)
        {
            var inverseShaderResource = FindResource("Shader_Inverse");
            if (!(inverseShaderResource is Effect inverseShader))
                throw new Exception("Missing inverse shader resource.");

            Effect = Effect != inverseShader ? inverseShader : null;
        }
    }
}
