using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Miniräknare.Expressions;
using Miniräknare.Expressions.Tokens;

namespace Miniräknare
{
    public partial class ExpressionBox : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty TextValueProperty = DependencyProperty.Register(
             nameof(TextValue),
             typeof(string),
             typeof(ExpressionBox),
             new PropertyMetadata(""));

        public event PropertyChangedEventHandler PropertyChanged;

        private ExpressionTree _expressionTree;
        private HashSet<ExpressionBox> _references;

        private ExpressionTreeProbe _probe;
        private ExpressionTreeEvaluator _evaluator;

        private string _variableName;
        private bool _showName;
        private bool _isNameEnabled;

        private ExpressionBoxState _state;
        private Evaluation _resultValue;

        #region Notifying Properties

        public string VariableName
        {
            get => _variableName;
            set
            {
                if (_variableName != value && ValidateName(value.AsMemory(), out var newName))
                {
                    string oldName = _variableName;

                    if (oldName != null)
                        MainWindow.ExpressionBoxes.Remove(oldName.AsMemory());

                    if (!newName.IsEmpty)
                        MainWindow.ExpressionBoxes.Add(newName, this);

                    _variableName = newName.ToString();
                    InvokePropertyChanged();

                    foreach (var box in MainWindow.ExpressionBoxes.Values)
                        box.UpdateResultValue();
                }
            }
        }

        public bool ShowName
        {
            get => _showName;
            set
            {
                if (_showName != value)
                {
                    _showName = value;
                    InvokePropertyChanged();

                    InvokePropertyChanged(nameof(NameVisibility));
                    InvokePropertyChanged(nameof(NameColumnMinWidth));
                }
            }
        }

        public bool IsNameEnabled
        {
            get => _isNameEnabled;
            set
            {
                if (_isNameEnabled != value)
                {
                    _isNameEnabled = value;
                    InvokePropertyChanged();
                }
            }
        }

        public ExpressionBoxState State
        {
            get => _state;
            set
            {
                if (_state != value)
                {
                    _state = value;
                    InvokePropertyChanged();
                }
            }
        }

        public string TextValue
        {
            get => (string)GetValue(TextValueProperty);
            set
            {
                if (TextValue != value)
                {
                    SetValue(TextValueProperty, value);
                    InvokePropertyChanged();

                    State = ParseTextValue();
                    if (State == ExpressionBoxState.Ok)
                        UpdateResultValue();
                }
            }
        }

        public Evaluation ResultValue
        {
            get => _resultValue;
            set
            {
                if (!_resultValue.Equals(value))
                {
                    _resultValue = value;
                    InvokePropertyChanged();
                }
            }
        }

        #endregion

        public Visibility NameVisibility => ShowName ? Visibility.Visible : Visibility.Collapsed;
        public double NameColumnMinWidth => ShowName ? 40 : 0;

        public ExpressionBox()
        {
            InitializeComponent();

            _state = ExpressionBoxState.Indeterminate;
            _isNameEnabled = true;
            _showName = true;

            _probe = new ExpressionTreeProbe();
            _probe.ProbeReference += ProbeReference;

            _evaluator = new ExpressionTreeEvaluator(
                ResolveReference,
                ResolveOperator,
                ResolveFunction);

            _references = new HashSet<ExpressionBox>();
        }

        public ExpressionBox(ExpressionOptions options) : this()
        {
            _expressionTree = new ExpressionTree(options);

        }

        public ExpressionBoxState ParseTextValue()
        {
            if (_expressionTree == null)
                return ExpressionBoxState.Indeterminate;

            _expressionTree.Tokens.Clear();
            ExpressionTokenizer.Tokenize(TextValue.AsMemory(), _expressionTree.Tokens);

            var sanitizeResult = ExpressionSanitizer.Sanitize(_expressionTree);
            if (sanitizeResult.Code != ExpressionSanitizer.ResultCode.Ok)
                return ExpressionBoxState.SyntaxError;

            var parseCode = ExpressionParser.Parse(_expressionTree);
            switch (parseCode)
            {
                case ExpressionParser.ResultCode.Ok:
                case ExpressionParser.ResultCode.NoTokens:
                    return ExpressionBoxState.Ok;

                default:
                    // TODO: add more exact error object
                    return ExpressionBoxState.SyntaxError;
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

            if (HasCyclicReferences(VariableName, _references))
            {
                State = ExpressionBoxState.CyclicReferences;
                return new Evaluation(EvalCode.CyclicReferences);
            }

            var eval = _evaluator.Evaluate(_expressionTree);
            return eval;
        }

        public static bool ValidateName(
            ReadOnlyMemory<char> newName,
            out ReadOnlyMemory<char> validatedName)
        {
            validatedName = newName.Trim();

            if (validatedName.IsEmpty)
                return true;

            var nameSpan = validatedName.Span;
            for (int i = 0; i < nameSpan.Length; i++)
            {
                char c = nameSpan[i];
                if (!(
                    ExpressionTokenizer.IsNameToken(c) ||
                    ExpressionTokenizer.IsSpaceToken(c) ||
                    (i > 0 && ExpressionTokenizer.IsDigitToken(c))))
                    return false;
            }

            if (MainWindow.ExpressionBoxes.ContainsKey(validatedName))
                return false;

            return true;
        }

        private static bool HasCyclicReferences(
            string baseName, IEnumerable<ExpressionBox> baseReferences)
        {
            var checkedSet = new HashSet<ExpressionBox>();

            bool Core(string name, IEnumerable<ExpressionBox> references)
            {
                foreach (var reference in references)
                {
                    if (name == reference.VariableName)
                        return true;

                    if (checkedSet.Add(reference) && Core(name, reference._references))
                        return true;
                }
                return false;
            }

            if (Core(baseName, baseReferences))
                return true;

            foreach (var set in checkedSet)
                if (set.State == ExpressionBoxState.CyclicReferences)
                    return true;

            return false;
        }



        private void ProbeReference(ValueToken name)
        {
            if (!MainWindow.ExpressionBoxes.TryGetValue(name.Value, out var field))
                return;

            if (_references.Add(field))
                field.PropertyChanged += ReferenceChanged;
        }


        private static ExpressionBoxState EvalToFieldState(EvalCode code, UnionValue value)
        {
            switch (code)
            {
                case EvalCode.Empty:
                case EvalCode.Ok:
                    return ExpressionBoxState.Ok;

                case EvalCode.CyclicReferences:
                    return ExpressionBoxState.CyclicReferences;

                case EvalCode.UnresolvedFunction:
                    return ExpressionBoxState.UnknownFunction;

                case EvalCode.InvalidArguments:
                case EvalCode.InvalidArgumentCount:
                    return ExpressionBoxState.InvalidArguments;

                case EvalCode.UnresolvedOperator:
                case EvalCode.UnresolvedReference:
                    return ExpressionBoxState.UnknownWord;

                case EvalCode.ErroredFunction:
                case EvalCode.ErroredOperator:
                case EvalCode.ErroredReference:
                    if (value.Type == UnionValueType.Enum)
                    {
                        var state = (ExpressionBoxState)value.Enum;
                        state &= ~ExpressionBoxState.NestedError;
                        switch (state)
                        {
                            case ExpressionBoxState.UnknownWord:
                            case ExpressionBoxState.UnknownFunction:
                            case ExpressionBoxState.SyntaxError:
                                return state | ExpressionBoxState.NestedError;
                        }
                    }
                    return ExpressionBoxState.Indeterminate;

                case EvalCode.Undefined:
                default:
                    return ExpressionBoxState.SyntaxError;
            }
        }

        public void UpdateResultValue()
        {
            var eval = Evaluate();
            State = EvalToFieldState(eval.Code, eval.Value);

            ResultValue = eval.Code == EvalCode.Ok ? eval : new Evaluation(new UnionValue(0d));
        }

        private Evaluation ResolveReference(ReadOnlyMemory<char> name)
        {
            if (!MainWindow.ExpressionBoxes.TryGetValue(name, out var field))
                return new Evaluation(EvalCode.UnresolvedReference, name);

            if (field.State != ExpressionBoxState.Ok)
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

                case '-':
                case '–':
                    return left.GetValueOrDefault().Double - right.Value.Double;

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

        private void TextValueBox_Loaded(object sender, RoutedEventArgs e)
        {
            DataObject.AddPastingHandler((TextBox)sender, OnPaste);
        }

        public void TextValueBox_Unloaded(object sender, RoutedEventArgs args)
        {
            DataObject.RemovePastingHandler((TextBox)sender, OnPaste);
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

        private void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            // TODO: implement this
            return;

            var isText = e.SourceDataObject.GetDataPresent(DataFormats.Html, true);
            if (!isText)
                return;

            var html = e.SourceDataObject.GetData(DataFormats.Html) as string;
        }

        protected void InvokePropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                var args = new PropertyChangedEventArgs(propertyName);
                PropertyChanged.Invoke(this, args);
            }
        }
    }
}
