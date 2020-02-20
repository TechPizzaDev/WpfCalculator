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
    [DefaultBindingProperty(nameof(TextValue))]
    public partial class ExpressionBox : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _lastVariableName;

        private ExpressionTree _expressionTree;
        private ExpressionTreeProbe _probe;
        private ExpressionTreeEvaluator _evaluator;
        private HashSet<ExpressionBox> _references;

        private bool _showName;
        private ExpressionBoxState _state;
        private Evaluation _resultValue;

        #region Notifying Properties

        public bool ShowName
        {
            get => _showName;
            set
            {
                if (_showName != value)
                {
                    _showName = value;
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

        public bool IsVariableNameEnabled
        {
            get => NameBox.IsEnabled;
            set => NameBox.IsEnabled = value;
        }

        public string VariableName
        {
            get => NameBox.Text;
            set => NameBox.Text = value;
        }

        public string TextValue
        {
            get => ValueBox.Text;
            set => ValueBox.Text = TextValue;
        }

        public ExpressionTree ExpressionTree
        {
            get => _expressionTree;
            set
            {
                if (_expressionTree != value)
                {
                    _expressionTree = value;
                    UpdateResultValue();
                }
            }
        }

        #region Constructors

        public ExpressionBox()
        {
            InitializeComponent();

            NameBox.TextChanged += NameBox_TextChanged;
            ValueBox.TextChanged += ValueBox_TextChanged;

            State = ExpressionBoxState.Indeterminate;
            IsVariableNameEnabled = true;
            ShowName = true;

            _expressionTree = new ExpressionTree(ExpressionOptions.Default);

            _probe = new ExpressionTreeProbe();
            _probe.ProbeReference += ProbeReference;

            _evaluator = new ExpressionTreeEvaluator(
                ResolveReference,
                ResolveOperator,
                ResolveFunction);

            _references = new HashSet<ExpressionBox>();
        }

        #endregion

        private void NameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var box = (TextBox)sender;
            if (ValidateName(box.Text.AsMemory(), out ReadOnlyMemory<char> newName))
            {
                if (_lastVariableName != null)
                    MainWindow.GlobalExpressions.Remove(_lastVariableName.AsMemory());

                if (!newName.IsEmpty)
                    MainWindow.GlobalExpressions.Add(newName, this);

                var newNameString = newName.ToString();
                _lastVariableName = newNameString;
                box.Text = newNameString;
                InvokePropertyChanged(nameof(VariableName));

                foreach (var globalBox in MainWindow.GlobalExpressions.Values)
                    globalBox.UpdateResultValue();
            }
            else
            {
                box.Text = _lastVariableName;
            }
        }

        private void ValueBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            InvokePropertyChanged(nameof(TextValue));

            State = ParseTextValue();
            if (State == ExpressionBoxState.Ok)
                UpdateResultValue();
        }

        public ExpressionBoxState ParseTextValue()
        {
            if (ExpressionTree == null)
                return ExpressionBoxState.Indeterminate;

            ExpressionTree.Tokens.Clear();
            ExpressionTokenizer.Tokenize(TextValue.AsMemory(), ExpressionTree.Tokens);

            var sanitizeResult = ExpressionSanitizer.Sanitize(ExpressionTree);
            if (sanitizeResult.Code != ExpressionSanitizer.ResultCode.Ok)
                return ExpressionBoxState.SyntaxError;

            var parseCode = ExpressionParser.Parse(ExpressionTree);
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
            if (ExpressionTree == null)
                return Evaluation.Undefined;

            foreach (var field in _references)
                field.PropertyChanged -= ReferenceChanged;
            _references.Clear();

            _probe.Probe(ExpressionTree);

            if (HasCyclicReferences(VariableName, _references))
            {
                State = ExpressionBoxState.CyclicReferences;
                return new Evaluation(EvalCode.CyclicReferences);
            }

            var eval = _evaluator.Evaluate(ExpressionTree);
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

            if (MainWindow.GlobalExpressions.ContainsKey(validatedName))
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
            if (!MainWindow.GlobalExpressions.TryGetValue(name.Value, out var field))
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
            if (!MainWindow.GlobalExpressions.TryGetValue(name, out var field))
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

        private void ValueBox_Loaded(object sender, RoutedEventArgs e)
        {
            DataObject.AddPastingHandler((TextBox)sender, OnPaste);
        }

        public void ValueBox_Unloaded(object sender, RoutedEventArgs args)
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
            switch (propertyName)
            {
                case nameof(ShowName):
                    NameColumn.MinWidth = ShowName ? 40 : 0;
                    NameBox.Visibility = ShowName ? Visibility.Visible : Visibility.Collapsed;
                    break;

                case nameof(IsVariableNameEnabled):
                    NameBox.IsEnabled = IsVariableNameEnabled;
                    break;
            }

            if (PropertyChanged != null)
            {
                var args = new PropertyChangedEventArgs(propertyName);
                PropertyChanged.Invoke(this, args);
            }
        }
    }
}
