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
    public partial class ExpressionField : UserControl, IContextMenuOperand, IHasContextMenu
    {
        public const string OkShader = "Shader_StateIcon_Ok";
        public const string ErrorShader = "Shader_StateIcon_Error";
        public const string EvalErrorShader = "Shader_StateIcon_EvalError";
        public const string NestedErrorShader = "Shader_StateIcon_NestedError";

        public ExpressionField()
        {
            InitializeComponent();

            InputBox.PropertyChanged += (s, e) => InputBox_PropertyChanged(s, e, ResultBox, StateImage);
        }

        public static void InputBox_PropertyChanged(
            object sender,
            PropertyChangedEventArgs e,
            TextBox resultOutput,
            Image stateImage)
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
                    double childValue = child.GetValueOrDefault().Double;

                    // Cast to decimal to provide "more precise" answers.
                    if (childValue > (double)decimal.MinValue &&
                        childValue < (double)decimal.MaxValue)
                        builder.Append((decimal)childValue);
                    else
                        builder.Append(childValue);

                    if (appendSeparator)
                        builder.Append(ExpressionTokenizer.ListSeparatorChar).Append(" ");
                }
                else if (collection.Children != null)
                {
                    builder.Append(ExpressionTokenizer.ListOpeningChar);

                    var children = collection.Children;
                    for (int i = 0; i < children.Length; i++)
                    {
                        Append(children[i], appendSeparator: i < children.Length - 1);
                    }
                    builder.Append(ExpressionTokenizer.ListClosingChar);
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
                    Console.WriteLine(InputBox.VariableName + ": " + "Remove");
                    break;
            }
        }
    }
}