using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Miniräknare
{
    public partial class ExpressionField : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ExpressionField()
        {
            InitializeComponent();

            InputBox.PropertyChanged += (s, e) => InputBox_PropertyChanged(s, e, ResultBox, StateImage);
        }

        protected void InvokePropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                var args = new PropertyChangedEventArgs(propertyName);
                PropertyChanged.Invoke(this, args);
            }
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

                    stateImage.Source = (ImageSource)GetStatusIconResource(box.State);
                    break;

                case nameof(ExpressionBox.ResultValue):
                    UpdateResultValueText(resultOutput, box);
                    break;
            }
        }

        public static void UpdateResultValueText(TextBox output, ExpressionBox source)
        {
            output.Text = source.State == ExpressionBoxState.Ok
                ? source.ResultValue.Value.ToString(false)
                : string.Empty;
        }

        public static object GetStatusIconResource(ExpressionBoxState state)
        {
            if (state == ExpressionBoxState.Indeterminate)
                return null;

            string resourceName = "Icon_" + state;
            return App.Instance.MainWindow.FindResource(resourceName);
        }
    }
}