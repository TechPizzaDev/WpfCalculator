using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Miniräknare.Expressions;

namespace Miniräknare
{
    public partial class ExpressionField : INotifyPropertyChanged
    {
        private ExpressionTree _expressionTree;
        private HashSet<ExpressionField> _references;

        private bool _isHitTestVisible;
        private string _name;

        private string _textValue;
        private FieldState _state;
        private Evaluation _resultValue;

        public event PropertyChangedEventHandler PropertyChanged;

        public int TabIndex { get; set; }
        public object StateIcon { get; private set; }
        public string ResultTextValue { get; private set; }

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

                    var newStateIcon = GetStatusIconResource(_state);
                    if (newStateIcon != StateIcon)
                    {
                        StateIcon = newStateIcon;
                        OnPropertyChanged(nameof(StateIcon));
                    }
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

                    State = ParseTextValue();
                    if (State == FieldState.Ok)
                        UpdateResultValue();
                }
            }
        }

        public Evaluation ResultValue
        {
            get
            {
                return _resultValue;
            }
            set
            {
                if (!_resultValue.Equals(value))
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
                        field.UpdateResultValue();
                }
            }
        }

        #endregion

        public ExpressionField()
        {
            _references = new HashSet<ExpressionField>();

            IsHitTestVisible = true;
            TextValue = string.Empty;
            State = FieldState.Ok;
        }

        public ExpressionField(string name, ExpressionOptions options) : this()
        {
            if (!ValidateName(name, out string validatedName))
                throw new ArgumentException(nameof(name));

            _expressionTree = new ExpressionTree(options);
            Name = validatedName;

            UpdateResultValue();
        }

        public FieldState ParseTextValue()
        {
            if (_expressionTree == null)
                return FieldState.Indeterminate;

            _expressionTree.Tokens.Clear();
            ExpressionTokenizer.Tokenize(TextValue.AsMemory(), _expressionTree.Tokens);

            var sanitizeResult = ExpressionSanitizer.Sanitize(_expressionTree);
            if (sanitizeResult.Code != ExpressionSanitizer.ResultCode.Ok)
                return FieldState.SyntaxError;

            var parseCode = ExpressionParser.Parse(_expressionTree);
            if (parseCode == ExpressionParser.ResultCode.NoTokens)
                return FieldState.Ok;

            if (parseCode != ExpressionParser.ResultCode.Ok)
                return FieldState.SyntaxError;

            return FieldState.Ok;
        }

        private static FieldState EvalToFieldState(EvalCode code, UnionValue value)
        {
            switch (code)
            {
                case EvalCode.Empty:
                case EvalCode.Ok:
                    return FieldState.Ok;

                case EvalCode.UnresolvedFunction:
                    return FieldState.UnknownFunction;

                case EvalCode.UnresolvedOperator:
                case EvalCode.UnresolvedReference:
                    return FieldState.UnknownWord;

                case EvalCode.ErroredFunction:
                case EvalCode.ErroredOperator:
                case EvalCode.ErroredReference:
                    if (value.Type == UnionValueType.Enum)
                    {
                        var state = (FieldState)value.Enum;
                        switch (state)
                        {
                            case FieldState.UnknownWord:
                            case FieldState.UnknownFunction:
                            case FieldState.SyntaxError:
                                return state | FieldState.NestedError;
                        }
                    }
                    return FieldState.Indeterminate;

                case EvalCode.Undefined:
                default:
                    return FieldState.SyntaxError;
            }
        }

        public Evaluation Evaluate()
        {
            if (_expressionTree == null)
                return Evaluation.Undefined;

            var evaluator = new ExpressionTreeEvaluator(
                _expressionTree,
                ResolveReference,
                ResolveOperator,
                ResolveFunction);

            var eval = evaluator.Evaluate();
            return eval;

            //        foreach (var referenceField in MainWindow.Fields.Values)
            //        {
            //            if (referenceField.Name != part)
            //                continue;

            //            if (referenceField == this)
            //            {
            //                currentState = FieldState.CyclicReferences;
            //                break;
            //            }

            //if (CheckForCyclicReferences(Name, newReferences))
            //{
            //    State = FieldState.CyclicReferences;
            //    return;
            //}
        }

        private void UpdateResultValue()
        {
            foreach (var field in _references)
                field.PropertyChanged -= ReferenceChanged;
            _references.Clear();

            var eval = Evaluate();
            State = EvalToFieldState(eval.Code, eval.Value);

            ResultValue = eval.Code == EvalCode.Ok ? eval : new Evaluation(new UnionValue(0d));
            ResultTextValue = State == FieldState.Ok ? ResultValue.Value.ToString(false) : "";

            OnPropertyChanged(nameof(ResultTextValue));
        }

        private Evaluation ResolveReference(ReadOnlyMemory<char> name)
        {
            if (!MainWindow.Fields.TryGetValue(name.ToString(), out var field))
                return new Evaluation(EvalCode.UnresolvedReference, name);

            if (_references.Add(field))
                field.PropertyChanged += ReferenceChanged;

            if (field.State != FieldState.Ok)
                return new Evaluation(
                    EvalCode.ErroredReference, UnionValue.FromEnum(field.State), name);

            return field.ResultValue;
        }

        private Evaluation ResolveFunction(ReadOnlyMemory<char> name, ReadOnlySpan<UnionValue> arguments)
        {
            return new Evaluation(EvalCode.UnresolvedFunction, name);
        }

        public static Evaluation ResolveOperator(ReadOnlyMemory<char> name, UnionValue? left, UnionValue? right)
        {
            if (name.Length != 1)
                return new Evaluation(EvalCode.UnresolvedOperator, name);

            switch (name.Span[0])
            {
                case '+': return left.GetValueOrDefault().Double + right.Value.Double;
                case '-': return left.GetValueOrDefault().Double - right.Value.Double;

                case '*': return left.Value.Double * right.Value.Double;

                case ':':
                case '/': return left.Value.Double / right.Value.Double;

                case '%': return left.Value.Double % right.Value.Double;

                case '^': return Math.Pow(left.Value.Double, right.Value.Double);

                case '!':
                    double number = left.Value.Double;
                    double result = 1;
                    while (number != 1)
                    {
                        result *= number;
                        number -= 1;
                    }
                    return result;

                default:
                    return new Evaluation(EvalCode.UnresolvedOperator, name);
            }
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
                UpdateResultValue();
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

        public static bool ValidateName(string newName, out string validatedName)
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

        public static object GetStatusIconResource(FieldState state)
        {
            if (state == FieldState.Indeterminate)
                return null;

            string resourceName = "Icon_" + state;
            return App.Instance.MainWindow.FindResource(resourceName);
        }
    }
}