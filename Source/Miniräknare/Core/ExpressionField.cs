using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Miniräknare.Expressions;

namespace Miniräknare
{
    public partial class ExpressionField : INotifyPropertyChanged
    {
        private bool _isHitTestVisible;

        public event PropertyChangedEventHandler PropertyChanged;

        public int TabIndex { get; set; }
        public object StateIcon { get; private set; }

        public ExpressionBox InputBox { get; }

        public string ResultTextValue { get; private set; }
        public Visibility ResultValueVisibility { get; private set; }

        #region Notifying Properties

        public bool IsHitTestVisible
        {
            get => _isHitTestVisible;
            set
            {
                if (_isHitTestVisible != value)
                {
                    _isHitTestVisible = value;
                    InvokePropertyChanged();
                }
            }
        }

        #endregion

        private ExpressionField(bool createExpressionBox)
        {
            if (createExpressionBox)
                InputBox = new ExpressionBox();
            _isHitTestVisible = true;
        }

        public ExpressionField() : this(true)
        {
        }

        public ExpressionField(ExpressionOptions options) : this(false)
        {
            InputBox = new ExpressionBox(options);
            InputBox.PropertyChanged += InputBox_PropertyChanged;

            InputBox.UpdateResultValue();
        }

        private void InputBox_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var box = sender as ExpressionBox;
            switch (e.PropertyName)
            {
                case nameof(ExpressionBox.State):
                    ResultValueVisibility = box.State == ExpressionBoxState.Ok ? Visibility.Visible : Visibility.Hidden;
                    InvokePropertyChanged(nameof(ResultValueVisibility));

                    var newStateIcon = GetStatusIconResource(box.State);
                    if (newStateIcon != StateIcon)
                    {
                        StateIcon = newStateIcon;
                        InvokePropertyChanged(nameof(StateIcon));
                    }
                    break;


                case nameof(ExpressionBox.ResultValue):
                    ResultTextValue = box.State == ExpressionBoxState.Ok ? box.ResultValue.Value.ToString(false) : "";
                    InvokePropertyChanged(nameof(ResultTextValue));
                    break;
            }
        }

        protected void InvokePropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                var args = new PropertyChangedEventArgs(propertyName);
                PropertyChanged.Invoke(this, args);
            }
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