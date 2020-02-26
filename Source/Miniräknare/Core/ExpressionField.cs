using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Miniräknare
{
    public partial class ExpressionField : UserControl, INotifyPropertyChanged
    {
        public const string OkShader = "Shader_StateIcon_Ok";
        public const string ErrorShader = "Shader_StateIcon_Error";
        public const string EvalErrorShader = "Shader_StateIcon_EvalError";
        public const string NestedErrorShader = "Shader_StateIcon_NestedError";

        public event PropertyChangedEventHandler PropertyChanged;

        public ExpressionField()
        {
            InitializeComponent();

            InputBox.PropertyChanged += (s, e) => InputBox_PropertyChanged(s, e, ResultBox, StateImage);
        }

        public static void InputBox_PropertyChanged(
            object sender, PropertyChangedEventArgs e,
            TextBox resultOutput, Image stateImage)
        {
            var box = sender as ExpressionBox;
            switch (e.PropertyName)
            {
                case nameof(ExpressionBox.State):
                    resultOutput.Visibility = box.State == ExpressionBoxState.Ok ? Visibility.Visible : Visibility.Hidden;
                    UpdateResultValueText(resultOutput, box);

                    var (image, effect) = GetStatusResources(box.State);
                    stateImage.Source = image;
                    stateImage.Effect = effect;
                    break;

                case nameof(ExpressionBox.ResultValue):
                    UpdateResultValueText(resultOutput, box);
                    break;
            }
        }

        public static void UpdateResultValueText(TextBox output, ExpressionBox source)
        {
            string text = string.Empty;
            if (source.State == ExpressionBoxState.Ok)
            {
                var result = source.ResultValue.Value;
                if (result.Type == UnionValueType.Double ||
                    result.Type == UnionValueType.Float)
                {
                    // TODO: add decimal rounding setting

                    var resultDouble = result.ToDouble();
                    text = resultDouble.ToString();
                }
                else
                {
                    text = result.ToString(false);
                }
            }
            output.Text = text;
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
    }
}