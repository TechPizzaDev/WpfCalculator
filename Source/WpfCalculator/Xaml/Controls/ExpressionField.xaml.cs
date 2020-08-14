using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Newtonsoft.Json.Linq;
using WpfCalculator.Expressions;

namespace WpfCalculator
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

            InputBox.PropertyChanged += (s, e) =>
            {
                InputBox_PropertyChanged(s, e, ResultBox, StateImage);
            };
        }

        public static void InputBox_PropertyChanged(
            object sender,
            PropertyChangedEventArgs e,
            TextBox resultOutput,
            Image stateImage)
        {
            if (!(sender is ExpressionBox box))
                return;

            switch (e.PropertyName)
            {
                case nameof(ExpressionBox.Error):
                    resultOutput.Visibility = box.Error == null
                        ? Visibility.Visible
                        : Visibility.Hidden;

                    var (image, effect) = GetStatusResources(box.Error);
                    stateImage.Source = image;
                    stateImage.Effect = effect;

                    UpdateResultValueText(resultOutput, box);
                    break;

                case nameof(ExpressionBox.CurrentEvaluation):
                    UpdateResultValueText(resultOutput, box);
                    break;
            }
        }

        public static void UpdateResultValueText(TextBox output, ExpressionBox source)
        {
            var builder = new StringBuilder();

            void Append(JToken token, bool appendSeparator)
            {
                if (token.IsNumber())
                {
                    double value = token.Value<double>();

                    // Cast to decimal to provide "more precise" answers.
                    if (value > (double)decimal.MinValue &&
                        value < (double)decimal.MaxValue)
                        builder.Append((decimal)value);
                    else
                        builder.Append(value);

                    if (appendSeparator)
                        builder.Append(ExpressionTokenizer.ListSeparatorChar).Append(" ");
                }
                else if (token is JArray array)
                {
                    builder.Append(ExpressionTokenizer.ListOpeningChar);

                    for (int i = 0; i < array.Count; i++)
                    {
                        Append(array[i], appendSeparator: i < array.Count - 1);
                    }
                    builder.Append(ExpressionTokenizer.ListClosingChar);
                }
                else
                {
                    builder.Append(token);
                }
            }

            if (source.Result != null)
            {
                Append(source.Result, false);
            }

            output.Text = builder.ToString();
        }

        public static (ImageSource?, ShaderEffect?) GetStatusResources(EError? error)
        {
            // TODO: add type remapping (and map empty to ok)
            if (error.ContainsError(EErrorCode.Empty))
                error = null;

            string shaderName;
            if (error != null && error.InnerError != null)
            {
                while (error.InnerError != null)
                    error = error.InnerError;

                shaderName = NestedErrorShader;
            }
            else
            {
                switch (error?.Id)
                {
                    case nameof(EErrorCode.SyntaxError):
                    case nameof(EErrorCode.CyclicReference):
                        shaderName = ErrorShader;
                        break;

                    case nameof(EErrorCode.UnknownFunction):
                    case nameof(EErrorCode.UnknownOperator):
                    case nameof(EErrorCode.UnknownReference):
                    case nameof(EErrorCode.InvalidArguments):
                    case nameof(EErrorCode.InvalidArgumentCount):
                        shaderName = EvalErrorShader;
                        break;

                    default:
                        shaderName = OkShader;
                        break;
                }
            }
            var shader = App.Instance.MainWindow.FindResource(shaderName) as ShaderEffect;

            string imageId = error?.Id ?? "Ok";
            if (imageId == "InvalidArgumentCount")
                imageId = "InvalidArguments";

            string imageName = "Icon_Field_" + imageId;
            var image = App.Instance.MainWindow.TryFindResource(imageName) as ImageSource;

            if (image == null)
                Debug.WriteLine("Failed to find image resource for error \"" + imageId + "\"");

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