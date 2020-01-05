using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

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

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value && ValidateName(value, out value))
                {
                    //string oldName = _name;
                    _name = value;

                    OnPropertyChanged();

                    foreach (var field in MainWindow.Fields.Values)
                        field.Update();
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
            var currentState = FieldState.Ok;

            // TODO: put parsing of TextValue into seperate function,
            // so it isn't re-parsed everytime we want to update the ResultValue

            double sum = 0;
            var parts = TextValue.Split('+', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();

            var newReferences = new HashSet<ExpressionField>(parts.Where(x => !double.TryParse(x, out _)).Select(x => MainWindow.Fields[x]));

            foreach (var reference in _references)
            {
                if (newReferences.Contains(reference))
                    continue;

                reference.PropertyChanged -= ReferenceChanged;
                _references.Remove(reference);
            }

            foreach (string rawPart in parts)
            {
                if (currentState != FieldState.Ok)
                    break;

                string part = rawPart.Trim();

                if (!double.TryParse(part, out double partValue))
                {
                    bool hasFoundItem = false;

                    foreach (var referenceField in MainWindow.Fields.Values)
                    {
                        if (referenceField.Name != part)
                            continue;
                        hasFoundItem = true;

                        if (referenceField == this)
                        {
                            currentState = FieldState.CyclicReferences;
                            break;
                        }

                        // Add reference even if there are state errors;
                        // we can then track errored fields and show error messages.
                        if (newReferences.Add(referenceField))
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

            if (CheckForCyclicReferences(Name, newReferences))
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

        private void ReferenceChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == nameof(ResultValue) ||
                args.PropertyName == nameof(State))
            {
                Update();
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                var args = new PropertyChangedEventArgs(propertyName);
                PropertyChanged.Invoke(this, args);
            }
        }

        public bool ValidateName(string newName, out string validatedName)
        {
            validatedName = newName.Trim();

            if (string.IsNullOrWhiteSpace(validatedName))
                return false;

            foreach (var field in MainWindow.Fields.Values)
                if (field.Name == validatedName)
                    return false;

            return true;
        }

        private void NameBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && sender is TextBox textBox)
            {
                // Unfocus the text box and call it's binding.

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
