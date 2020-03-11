using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Miniräknare.Expressions;

namespace Miniräknare
{
    public partial class ExpressionField : UserControl, IHasContextMenu
    {
        private readonly (string, RoutedEventHandler)[] _contextMenuHandlers;

        public const string OkShader = "Shader_StateIcon_Ok";
        public const string ErrorShader = "Shader_StateIcon_Error";
        public const string EvalErrorShader = "Shader_StateIcon_EvalError";
        public const string NestedErrorShader = "Shader_StateIcon_NestedError";

        public ExpressionField()
        {
            InitializeComponent();

            InputBox.PropertyChanged += (s, e) => InputBox_PropertyChanged(s, e, ResultBox, StateImage);

            _contextMenuHandlers = new (string, RoutedEventHandler)[]
            {
                ("Remove", RemoveMenuItem_Click)
            };
            ContextMenu.Opened += ContextMenu_Opened;
        }

        private Dictionary<string, MenuItem> _contextMenuItems;

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (_contextMenuItems == null)
            {
                _contextMenuItems = new Dictionary<string, MenuItem>();
            }
            else
            {
                foreach (var handler in _contextMenuHandlers)
                    _contextMenuItems[handler.Item1].Click -= handler.Item2;
                _contextMenuItems.Clear();
            }

            var menu = (ContextMenu)sender;
            foreach (var item in menu.Items)
            {
                if (item is MenuItem menuItem)
                    _contextMenuItems.Add(menuItem.Name, menuItem);
            }

            foreach (var handler in _contextMenuHandlers)
                _contextMenuItems[handler.Item1].Click += handler.Item2;
        }

        private void RemoveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("OK");
        }

        public static void InputBox_PropertyChanged(
            object sender, PropertyChangedEventArgs e,
            TextBox resultOutput, Image stateImage)
        {
            var box = sender as ExpressionBox;
            switch (e.PropertyName)
            {
                case nameof(ExpressionBox.State):
                    resultOutput.Visibility = box.State == ExpressionBoxState.Ok
                        ? Visibility.Visible : Visibility.Hidden;

                    UpdateResultValueText(resultOutput, box);

                    var (image, effect) = GetStatusResources(box.State);
                    stateImage.Source = image;
                    stateImage.Effect = effect;
                    break;

                case nameof(ExpressionBox.ResultEvaluation):
                    UpdateResultValueText(resultOutput, box);
                    break;
            }
        }

        public static void UpdateResultValueText(TextBox output, ExpressionBox source)
        {
            var builder = new StringBuilder();

            void Append(in UnionValueCollection collection, bool appendSeparator)
            {
                var child = collection.Child;
                if (child != null)
                {
                    builder.Append(child.GetValueOrDefault().Double);
                    if (appendSeparator)
                        builder.Append(ExpressionTokenizer.ListSeparatorChar).Append(" ");
                }
                else if (collection.Children != null)
                {
                    builder.Append(ExpressionTokenizer.ListStartChar);

                    var children = collection.Children;
                    for (int i = 0; i < children.Length; i++)
                    {
                        Append(children[i], appendSeparator: i < children.Length - 1);
                    }
                    builder.Append(ExpressionTokenizer.ListEndChar);
                }
            }

            if (source.State == ExpressionBoxState.Ok)
            {
                Append(source.ResultEvaluation.Values, false);
            }
            output.Text = builder.ToString();
        }

        public static (ImageSource, ShaderEffect) GetStatusResources(ExpressionBoxState state)
        {
            if (state == ExpressionBoxState.Indeterminate)
                return (null, null);

            string shaderName;
            if (state.HasFlag(ExpressionBoxState.NestedError))
            {
                shaderName = NestedErrorShader;
            }
            else
            {
                switch (state)
                {
                    case ExpressionBoxState.SyntaxError:
                    case ExpressionBoxState.CyclicReferences:
                        shaderName = ErrorShader;
                        break;

                    case ExpressionBoxState.UnknownWord:
                    case ExpressionBoxState.UnknownFunction:
                    case ExpressionBoxState.InvalidArguments:
                        shaderName = EvalErrorShader;
                        break;

                    default:
                        shaderName = OkShader;
                        break;
                }
            }
            var shader = App.Instance.MainWindow.FindResource(shaderName) as ShaderEffect;

            string imageName = "Icon_Field_" + (state & ~ExpressionBoxState.NestedError);
            var image = App.Instance.MainWindow.FindResource(imageName) as ImageSource;

            return (image, shader);
        }

        private void OpenContextMenu(object sender, RoutedEventArgs e)
        {
            OpenContextMenu();
        }

        public void OpenContextMenu()
        {
            ContextMenu.IsOpen = true;
        }
    }
}