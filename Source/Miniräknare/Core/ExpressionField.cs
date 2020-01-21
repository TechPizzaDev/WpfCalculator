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

                    State = Parse();
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

        public FieldState Parse()
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

        private static FieldState EvalCodeToFieldState(EvalCode code)
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

                case EvalCode.Undefined:
                default:
                    return FieldState.SyntaxError;
            }
        }

        public (Evaluation Eval, FieldState State) Evaluate()
        {
            if (_expressionTree == null)
                return (Evaluation.Undefined, FieldState.Indeterminate);

            var evaluator = new ExpressionTreeEvaluator(
                _expressionTree,
                ResolveReference,
                ResolveOperator,
                ResolveFunction);

            var eval = evaluator.Evaluate();
            var state = EvalCodeToFieldState(eval.Code);

            return (eval, state);

            // TODO: put parsing of TextValue into seperate function,
            // so it isn't re-parsed everytime we want to update the ResultValue

            //double sum = 0;
            //var parts = TextValue.Split('+', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
            //
            //var newReferences = new HashSet<ExpressionField>(
            //    parts
            //    .Where(x => !double.TryParse(x, out _))
            //    .Select(x => MainWindow.Fields[x]));
            //
            //foreach (var reference in _references)
            //{
            //    if (newReferences.Contains(reference))
            //        continue;
            //
            //    reference.PropertyChanged -= ReferenceChanged;
            //    _references.Remove(reference);
            //}
            //
            //foreach (string rawPart in parts)
            //{
            //    if (currentState != FieldState.Ok)
            //        break;
            //
            //    string part = rawPart.Trim();
            //
            //    if (!double.TryParse(part, out double partValue))
            //    {
            //        bool hasFoundItem = false;
            //
            //        foreach (var referenceField in MainWindow.Fields.Values)
            //        {
            //            if (referenceField.Name != part)
            //                continue;
            //            hasFoundItem = true;
            //
            //            if (referenceField == this)
            //            {
            //                currentState = FieldState.CyclicReferences;
            //                break;
            //            }
            //
            //            // Add reference even if there are state errors;
            //            // we can then track errored fields and show error messages.
            //            if (newReferences.Add(referenceField))
            //                referenceField.PropertyChanged += ReferenceChanged;
            //
            //            // Only change current state if it's Ok,
            //            // we don't want to overwrite previous errors.
            //            if (currentState == FieldState.Ok)
            //            {
            //                if (referenceField.State != FieldState.Ok)
            //                    currentState = referenceField.State.AsNested();
            //            }
            //
            //            if (currentState == FieldState.Ok)
            //            {
            //                partValue = 0;
            //                //referenceField.ResultValue is Evaluation fieldValue
            //                //    ? fieldValue
            //                //    : default;
            //
            //                currentState = FieldState.Ok;
            //            }
            //            break;
            //        }
            //
            //        if (!hasFoundItem)
            //        {
            //            currentState = FieldState.UnknownWords;
            //        }
            //    }
            //
            //    if (currentState == FieldState.Ok)
            //    {
            //        sum += partValue;
            //    }
            //}
            //
            //if (currentState != FieldState.Ok)
            //{
            //    State = currentState;
            //    return;
            //}
            //
            //if (CheckForCyclicReferences(Name, newReferences))
            //{
            //    State = FieldState.CyclicReferences;
            //    return;
            //}
            //
            //State = FieldState.Ok;
            //ResultValue = sum;
        }

        private void UpdateResultValue()
        {
            var (eval, state) = Evaluate();
            State = state;

            ResultValue = eval.Code != EvalCode.Empty
                ? eval : new Evaluation(new UnionValue(0d));

            ResultTextValue = State != FieldState.Ok
                ? "" : ResultValue.Value.ToString(false);

            OnPropertyChanged(nameof(ResultTextValue));
        }

        private Evaluation ResolveReference(ReadOnlyMemory<char> name)
        {
            return new Evaluation(EvalCode.UnresolvedReference);
            throw new NotImplementedException();
        }

        private Evaluation ResolveFunction(ReadOnlyMemory<char> name, ReadOnlySpan<UnionValue> arguments)
        {
            return new Evaluation(EvalCode.UnresolvedFunction);
        }

        public static Evaluation ResolveOperator(ReadOnlyMemory<char> name, UnionValue? left, UnionValue? right)
        {
            if (name.Length != 1)
                return new Evaluation(EvalCode.UnresolvedOperator);

            switch (name.Span[0])
            {
                case '+': return left.GetValueOrDefault().Double + right.Value.Double;
                case '-': return left.GetValueOrDefault().Double - right.Value.Double;

                case '*': return left.Value.Double * right.Value.Double;
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
                    return new Evaluation(EvalCode.UnresolvedOperator);
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
                Evaluate();
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
