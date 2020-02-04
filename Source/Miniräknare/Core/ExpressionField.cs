using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Miniräknare.Expressions;
using Miniräknare.Expressions.Tokens;

namespace Miniräknare
{
    public partial class ExpressionField : INotifyPropertyChanged
    {
        private ExpressionTree _expressionTree;
        private HashSet<ExpressionField> _references;

        private ExpressionTreeProbe _probe;
        private ExpressionTreeEvaluator _evaluator;

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
                if (_name != value && ValidateName(value.AsMemory(), out var newName))
                {
                    string oldName = _name;

                    if (oldName != null)
                        MainWindow.Fields.Remove(oldName.AsMemory());
                    MainWindow.Fields.Add(newName, this);

                    _name = newName.ToString();
                    OnPropertyChanged();

                    foreach (var field in MainWindow.Fields.Values)
                        field.UpdateResultValue();
                }
            }
        }

        #endregion

        public ExpressionField()
        {
            IsHitTestVisible = true;
            TextValue = string.Empty;
            State = FieldState.Ok;
        }

        public ExpressionField(ReadOnlyMemory<char> name, ExpressionOptions options) : this()
        {
            if (!ValidateName(name, out var validatedName))
                throw new ArgumentException(nameof(name));

            _probe = new ExpressionTreeProbe();
            _probe.ProbeReference += ProbeReference;

            _evaluator = new ExpressionTreeEvaluator(
                ResolveReference,
                ResolveOperator,
                ResolveFunction);

            _references = new HashSet<ExpressionField>();
            _expressionTree = new ExpressionTree(options);
            Name = validatedName.ToString();

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

                case EvalCode.CyclicReferences:
                    return FieldState.CyclicReferences;

                case EvalCode.UnresolvedFunction:
                    return FieldState.UnknownFunction;

                case EvalCode.InvalidArguments:
                case EvalCode.InvalidArgumentCount:
                    return FieldState.InvalidArguments;

                case EvalCode.UnresolvedOperator:
                case EvalCode.UnresolvedReference:
                    return FieldState.UnknownWord;

                case EvalCode.ErroredFunction:
                case EvalCode.ErroredOperator:
                case EvalCode.ErroredReference:
                    if (value.Type == UnionValueType.Enum)
                    {
                        var state = (FieldState)value.Enum;
                        state &= ~FieldState.NestedError;
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

            foreach (var field in _references)
                field.PropertyChanged -= ReferenceChanged;
            _references.Clear();

            _probe.Probe(_expressionTree);

            if (HasCyclicReferences(Name, _references))
            {
                State = FieldState.CyclicReferences;
                return new Evaluation(EvalCode.CyclicReferences);
            }

            var eval = _evaluator.Evaluate(_expressionTree);
            return eval;
        }

        private void ProbeReference(ValueToken name)
        {
            if (!MainWindow.Fields.TryGetValue(name.Value, out var field))
                return;

            if (_references.Add(field))
                field.PropertyChanged += ReferenceChanged;
        }

        private static bool HasCyclicReferences(
            string baseName, IEnumerable<ExpressionField> baseReferences)
        {
            var checkedSet = new HashSet<ExpressionField>();

            bool Core(string name, IEnumerable<ExpressionField> references)
            {
                foreach (var reference in references)
                {
                    if (name == reference.Name)
                        return true;

                    if (checkedSet.Add(reference) && Core(name, reference._references))
                        return true;
                }
                return false;
            }

            if (Core(baseName, baseReferences))
                return true;

            foreach (var set in checkedSet)
                if (set.State == FieldState.CyclicReferences)
                    return true;

            return false;
        }

        private void UpdateResultValue()
        {
            var eval = Evaluate();
            State = EvalToFieldState(eval.Code, eval.Value);

            ResultValue = eval.Code == EvalCode.Ok ? eval : new Evaluation(new UnionValue(0d));
            ResultTextValue = State == FieldState.Ok ? ResultValue.Value.ToString(false) : "";

            OnPropertyChanged(nameof(ResultTextValue));
        }

        private Evaluation ResolveReference(ReadOnlyMemory<char> name)
        {
            if (!MainWindow.Fields.TryGetValue(name, out var field))
                return new Evaluation(EvalCode.UnresolvedReference, name);

            if (field.State != FieldState.Ok)
                return new Evaluation(
                    EvalCode.ErroredReference, UnionValue.FromEnum(field.State), name);

            return field.ResultValue;
        }

        private Evaluation ResolveFunction(
            ReadOnlyMemory<char> name, ReadOnlySpan<UnionValue> arguments)
        {
            if (name.Span.SequenceEqual("sin"))
            {
                int expectedArgCount = 1;
                if (arguments.Length != expectedArgCount)
                    return new Evaluation(
                        EvalCode.InvalidArgumentCount, new UnionValue(expectedArgCount));

                return new Evaluation(new UnionValue(Math.Sin(arguments[0].Double)));
            }
            else if (name.Span.SequenceEqual("round"))
            {
                int expectedArgCount = 1;
                if (arguments.Length != expectedArgCount)
                    return new Evaluation(
                        EvalCode.InvalidArgumentCount, new UnionValue(expectedArgCount));

                return new Evaluation(new UnionValue(Math.Round(arguments[0].Double)));
            }
            return new Evaluation(EvalCode.UnresolvedFunction, name);
        }

        public static Evaluation ResolveOperator(
            ReadOnlyMemory<char> name, UnionValue? left, UnionValue? right)
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

        public static bool ValidateName(
            ReadOnlyMemory<char> newName,
            out ReadOnlyMemory<char> validatedName)
        {
            validatedName = newName.Trim();

            if (validatedName.IsEmpty)
                return false;

            var nameSpan = validatedName.Span;
            for (int i = 0; i < nameSpan.Length; i++)
            {
                char c = nameSpan[i];
                if (!(ExpressionTokenizer.IsNameToken(c) || ExpressionTokenizer.IsSpaceToken(c)))
                    return false;
            }

            if (MainWindow.Fields.ContainsKey(validatedName))
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