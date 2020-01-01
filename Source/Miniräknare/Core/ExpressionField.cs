using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Miniräknare
{
    public partial class ExpressionField : INotifyPropertyChanged
    {
        private HashSet<ExpressionField> _references;
        
        private string _textValue;
        private FieldState _state;
        private string _name;
        private object _resultValue;
        private bool _isHitTestVisible;
        
        public event PropertyChangedEventHandler PropertyChanged;

        public int TabIndex { get; set; }

        public object ErrorIcon { get; private set; }

        #region Notifying Properties

        public FieldState State
        {
            get => _state;
            set
            {
                if (_state != value)
                {
                    _state = value;
                    OnPropertyChanged();

                    SetErrorIcon(_state);
                }
            }
        }

        public string TextValue
        {
            get => _textValue;
            set
            {
                if (_textValue != value)
                {
                    _textValue = value;
                    OnPropertyChanged();

                    Update();
                }
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    return;

                value = ValidateName(value);
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();

                    foreach (var item in MainWindow.FieldListItems)
                        if (item.Content is ExpressionField field)
                            field.Update();
                }
            }
        }

        public object ResultValue
        {
            get => _resultValue;
            set
            {
                if (_resultValue != value)
                {
                    _resultValue = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsHitTestVisible
        {
            get => _isHitTestVisible;
            set
            {
                if (_isHitTestVisible != value)
                {
                    _isHitTestVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        public ExpressionField()
        {
            _references = new HashSet<ExpressionField>();

            IsHitTestVisible = true;
            TextValue = string.Empty;
        }

        public void Update()
        {
            ClearReferences();

            var currentState = FieldState.Ok;

            // TODO: put parsing of TextValue into seperate function,
            // so it isn't re-parsed everytime we want to update the ResultValue

            double sum = 0;
            string[] parts = TextValue.Split('+', StringSplitOptions.RemoveEmptyEntries);

            foreach (string rawPart in parts)
            {
                if (currentState != FieldState.Ok)
                    break;

                string part = rawPart.Trim();

                if (!double.TryParse(part, out double partValue))
                {
                    bool hasFoundItem = false;

                    foreach (var fieldListItem in MainWindow.FieldListItems)
                    {
                        if (!(fieldListItem.Content is ExpressionField referenceField))
                            continue;

                        if (referenceField.Name != part)
                            continue;
                        hasFoundItem = true;

                        if (fieldListItem.Content == this)
                        {
                            currentState = FieldState.CyclicReferences;
                            break;
                        }

                        // Add reference even if there are state errors;
                        // we can then track errored fields and show error messages.
                        if (_references.Add(referenceField))
                            referenceField.PropertyChanged += ReferenceChanged;

                        // Only change current state if it's Ok,
                        // we don't want to overwrite previous errors.
                        if (currentState == FieldState.Ok)
                        {
                            if (referenceField.State != FieldState.Ok)
                                currentState = referenceField.State.AsNested();
                        }

                        if (currentState == FieldState.Ok)
                        {
                            partValue = referenceField.ResultValue is double fieldValue
                                ? fieldValue
                                : default;

                            currentState = FieldState.Ok;
                        }
                        break;
                    }

                    if (!hasFoundItem)
                    {
                        currentState = FieldState.UnknownWords;
                    }
                }

                if (currentState == FieldState.Ok)
                {
                    sum += partValue;
                }
            }

            if (currentState != FieldState.Ok)
            {
                State = currentState;
                return;
            }

            if (CheckForCyclicReferences(Name, _references))
            {
                State = FieldState.CyclicReferences;
                return;
            }

            State = FieldState.Ok;
            ResultValue = sum;
        }

        private static bool CheckForCyclicReferences(
            string baseName, HashSet<ExpressionField> entryPoint)
        {
            if (entryPoint.Count == 0)
                return false;

            foreach (var entry in entryPoint)
            {
                if (entry.Name == baseName)
                    return true;

                if (CheckForCyclicReferences(baseName, entry._references))
                    return true;
            }
            return false;
        }

        private void ClearReferences()
        {
            foreach (var reference in _references)
                reference.PropertyChanged -= ReferenceChanged;
            _references.Clear();
        }

        private void ReferenceChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == nameof(ResultValue) ||
                args.PropertyName == nameof(State))
                Update();
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                var args = new PropertyChangedEventArgs(propertyName);
                PropertyChanged.Invoke(this, args);
            }
        }

        public string ValidateName(string name)
        {
            return name.Trim();
        }

        private void NameBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && sender is TextBox textBox)
            {
                Keyboard.ClearFocus();
                
                var binding = BindingOperations.GetBindingExpression(textBox, TextBox.TextProperty);
                binding?.UpdateSource();
            }
        }

        #region ErrorIcon Helpers

        private void SetErrorIcon(FieldState state)
        {
            var newErrorIconSource = GetErrorIconResource(state);
            if (newErrorIconSource != ErrorIcon)
            {
                ErrorIcon = newErrorIconSource;
                OnPropertyChanged(nameof(ErrorIcon));
            }
        }

        public static object GetErrorIconResource(FieldState state)
        {
            if (state == FieldState.Ok ||
                state == FieldState.Indeterminate)
                return null;

            string resourceName = "Icon_" + state;
            return App.Instance.MainWindow.FindResource(resourceName);
        }

        #endregion
    }
}
