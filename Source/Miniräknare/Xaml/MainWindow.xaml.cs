using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Miniräknare
{
    // TODO: settings:
    // smooth scroll in field list
    // show result values with base10 exponent (default) or just a lot of zeroes (+ max decimal count)

    public partial class MainWindow : Window
    {
        public static BindingList<ListViewItem> FieldList { get; } = new BindingList<ListViewItem>();
        public static Dictionary<string, ExpressionField> Fields { get; } = new Dictionary<string, ExpressionField>();

        private LanguageWindow _languageWindow;

        public MainWindow()
        {
            InitializeComponent();

            FieldListView.ItemsSource = FieldList;

            //InitializeDragDropManager();
        }

        private string GetNewFieldName()
        {
            foreach(var field in Fields.Keys)
            {

            }
            char[] omegalul = "qwertyuiopåasdfghjklöäzxcvbnm".ToCharArray();
            return omegalul[new Random().Next(omegalul.Length)].ToString();
        }

        #region DragDropManager stuff

        //private ListViewDragDropManager<ExpressionField> _dragDropManager;

        /*
        private void InitializeDragDropManager()
        {
            //_dragDropManager = new ListViewDragDropManager<ExpressionField>(FieldList);
            //_dragDropManager.DragAdornerOpacity = 0.8;
            //_dragDropManager.OnDragStart += DragDropManager_OnDragStart;
            //_dragDropManager.OnDragStop += DragDropManager_OnDragStop;
        }

        private void DragDropManager_OnDragStart(object sender, EventArgs e)
        {
            foreach (var field in _fields)
                field.IsHitTestVisible = false;

            Keyboard.ClearFocus();
        }

        private void DragDropManager_OnDragStop(object sender, EventArgs e)
        {
            foreach (var field in _fields)
                field.IsHitTestVisible = true;
        }
        */

        #endregion

        #region ActionButton Click handlers

        private void AddNewField_Click(object sender, RoutedEventArgs e)
        {
            var field = new ExpressionField
            {
                Name = GetNewFieldName()
            };

            var listItem = new ListViewItem
            {
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Content = field
            };

            FieldList.Add(listItem);
            Fields.Add(field.Name, field);
        }

        private void AddNewFunctionField_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            button.ContextMenu.IsOpen = true;
        }

        #endregion

        private void FieldList_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var listView = sender as ListView;
            listView.UnselectAll();
        }

        #region MenuItem Click handlers

        private void MenuItem_Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MenuItem_Language_Click(object sender, RoutedEventArgs e)
        {
            if (_languageWindow == null)
            {
                _languageWindow = new LanguageWindow();
                _languageWindow.Closed += LanguageWindow_Closed;
            }

            _languageWindow.Show();
            _languageWindow.Focus();
        }

        private void LanguageWindow_Closed(object sender, EventArgs e)
        {
            _languageWindow = null;
        }

        #endregion

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // TODO: check if there is stuff to save
        }
    }
}
