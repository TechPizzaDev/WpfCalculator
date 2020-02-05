using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Miniräknare.Expressions;

namespace Miniräknare
{
    // TODO: settings:
    //  * smooth scroll in field list (quality vs performance)
    //  * show result values with base10 exponent (default) or just a lot of zeroes (+ max decimal count)
    //  * result value with either comma or dot

    public partial class MainWindow : Window
    {
        private static readonly ReadOnlyMemory<char> _alphabet = "abcdefghijklmnopqrstuvwxyz".AsMemory();

        public static BindingList<ListViewItem> FieldList { get; } =
            new BindingList<ListViewItem>();

        public static Dictionary<ReadOnlyString, ExpressionField> Fields { get; } =
            new Dictionary<ReadOnlyString, ExpressionField>();

        private LanguageWindow _languageWindow;

        public MainWindow()
        {
            InitializeComponent();

            FieldListView.ItemsSource = FieldList;

            //InitializeDragDropManager();
        }

        private ReadOnlyMemory<char> GenerateFieldName()
        {
            const int maxLength = 10;
            var alphabet = _alphabet.Span;
            int targetIndex = alphabet.Length;

            int length = 1;
            int lastIndex = maxLength - 1;

            var nameBuffer = new char[maxLength];
            Span<int> indices = stackalloc int[maxLength];
            Memory<char> name;

            bool tryGet = true;
            do
            {
                for (int i = 0; i < length; i++)
                    nameBuffer[maxLength - i - 1] = alphabet[indices[maxLength - i - 1]];
                name = nameBuffer.AsMemory(maxLength - length, length);

                if (!Fields.ContainsKey(name))
                    break;

                indices[^1]++;

                for (int i = indices.Length; i-- > 0;)
                {
                    if (indices[i] != targetIndex)
                        continue;

                    if (i - 1 < 0)
                    {
                        if (indices[i] == targetIndex)
                        {
                            tryGet = false;
                            break;
                        }
                    }
                    else
                    {
                        indices[i - 1]++;
                    }

                    if (i < lastIndex)
                    {
                        length++;
                        if (length > maxLength)
                            throw new Exception("Max name length reached.");

                        lastIndex = i;
                    }
                    indices[i] = 0;
                }
            } while (tryGet);

            return name;
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
            var fieldName = GenerateFieldName();
            var field = new ExpressionField(fieldName, ExpressionOptions.Default);

            var listItem = new ListViewItem
            {
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Content = field
            };

            FieldList.Add(listItem);
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
