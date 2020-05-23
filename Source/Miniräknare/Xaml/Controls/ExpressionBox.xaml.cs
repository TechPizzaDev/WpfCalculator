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

        public Evaluation ResultEvaluation
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
            set
            {
                if (_lastVariableName == null)
                    _lastVariableName = value;

                if (NameBox.Text != value)
                {
                    NameBox.Text = value;
                    ValidateName();
                }
            }
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

            var cyclicReferences = HasCyclicReferences(VariableName, _references);
            if (cyclicReferences != CyclicReferenceType.None)
            {
                var newState = cyclicReferences == CyclicReferenceType.Nested
                    ? ExpressionBoxState.CyclicReferencesNested
                    : ExpressionBoxState.CyclicReferences;

                return new Evaluation(
                    EvalCode.ErroredReference, UnionValue.FromEnum(newState), VariableName.AsMemory());
            }

            var eval = _evaluator.EvaluateTree(ExpressionTree);
            return eval;
        }

        public static bool IsValidName(
            ReadOnlyString newName,
            out ReadOnlyString validatedName)
        {
            validatedName = newName.Chars.Trim();

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

        private static CyclicReferenceType HasCyclicReferences(
            string baseName, IEnumerable<ExpressionBox> baseReferences)
        {
            var checkedSet = new HashSet<ExpressionBox>();

            CyclicReferenceType Core(string name, IEnumerable<ExpressionBox> references)
            {
                foreach (var reference in references)
                {
                    if (name == reference.VariableName)
                        return CyclicReferenceType.Base;

                    if (checkedSet.Add(reference))
                    {
                        var core = Core(name, reference._references);
                        if (core != CyclicReferenceType.None)
                            return core;
                    }
                }
                return CyclicReferenceType.None;
            }

            var baseCore = Core(baseName, baseReferences);
            if (baseCore != CyclicReferenceType.None)
                return baseCore;

            foreach (var box in checkedSet)
            {
                var result = box.ResultEvaluation;
                if (result.Code == EvalCode.ErroredReference &&
                    ((ExpressionBoxState)result.Values.Child?.Enum).HasFlag(ExpressionBoxState.CyclicReferences))
                    return CyclicReferenceType.Nested;
            }
            return CyclicReferenceType.None;
        }

        public enum CyclicReferenceType
        {
            None,
            Base,
            Nested
        }

        private void ProbeReference(ValueToken name)
        {
            if (!MainWindow.GlobalExpressions.TryGetValue(name.Value, out var expression))
                return;

            if (_references.Add(expression))
                expression.PropertyChanged += ReferenceChanged;
        }


        private ExpressionBoxState EvalToFieldState(Evaluation eval)
        {
            switch (eval.Code)
            {
                case EvalCode.Ok:
                    return ExpressionBoxState.Ok;

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
                    var first = eval.Values.Child.GetValueOrDefault();
                    if (first.ValueType == UnionValueType.Enum)
                    {
                        var state = (ExpressionBoxState)first.Enum;
                        state &= ~ExpressionBoxState.NestedError;
                        switch (state)
                        {
                            case ExpressionBoxState.CyclicReferences:
                                if (eval.UnresolvedName.Span.SequenceEqual(VariableName))
                                    return state;
                                return state | ExpressionBoxState.NestedError;

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
            State = EvalToFieldState(eval);

            ResultEvaluation = eval.Code == EvalCode.Ok ? eval : new Evaluation(new UnionValue(0d));
        }

        private Evaluation ResolveReference(ReadOnlyMemory<char> name)
        {
            if (!MainWindow.GlobalExpressions.TryGetValue(name, out var field))
                return new Evaluation(EvalCode.UnresolvedReference, name);

            if (field.State != ExpressionBoxState.Ok)
                return new Evaluation(
                    EvalCode.ErroredReference, UnionValue.FromEnum(field.State), name);

            return field.ResultEvaluation;
        }

        private Evaluation ResolveFunction(
            ReadOnlyMemory<char> name, ReadOnlySpan<UnionValueCollection> arguments)
        {
            var firstArg = (arguments.Length > 0 ? arguments[0] : default).Child.GetValueOrDefault();
            var secondArg = (arguments.Length > 1 ? arguments[1] : default).Child.GetValueOrDefault();

            if (name.Span.SequenceEqual("sin"))
            {
                int expectedArgCount = 1;
                if (arguments.Length != expectedArgCount)
                    return new Evaluation(
                        EvalCode.InvalidArgumentCount, new UnionValue(expectedArgCount));

                return new Evaluation(new UnionValue(Math.Sin(firstArg.Double * 0.0174532925)));
            }
            else if (name.Span.SequenceEqual("cos"))
            {
                int expectedArgCount = 1;
                if (arguments.Length != expectedArgCount)
                    return new Evaluation(
                        EvalCode.InvalidArgumentCount, new UnionValue(expectedArgCount));

                return new Evaluation(new UnionValue(Math.Cos(firstArg.Double * 0.0174532925)));
            }
            else if (name.Span.SequenceEqual("tan"))
            {
                int expectedArgCount = 1;
                if (arguments.Length != expectedArgCount)
                    return new Evaluation(
                        EvalCode.InvalidArgumentCount, new UnionValue(expectedArgCount));

                return new Evaluation(new UnionValue(Math.Tan(firstArg.Double) * 57.2957795));
            }
            if (name.Span.SequenceEqual("asin") || name.Span.SequenceEqual("arcsin"))
            {
                int expectedArgCount = 1;
                if (arguments.Length != expectedArgCount)
                    return new Evaluation(
                        EvalCode.InvalidArgumentCount, new UnionValue(expectedArgCount));

                return new Evaluation(new UnionValue(Math.Asin(firstArg.Double) * 57.2957795));
            }
            else if (name.Span.SequenceEqual("acos") || name.Span.SequenceEqual("arccos"))
            {
                int expectedArgCount = 1;
                if (arguments.Length != expectedArgCount)
                    return new Evaluation(
                        EvalCode.InvalidArgumentCount, new UnionValue(expectedArgCount));

                return new Evaluation(new UnionValue(Math.Acos(firstArg.Double) * 57.2957795));
            }
            else if (name.Span.SequenceEqual("atan") || name.Span.SequenceEqual("arctan"))
            {
                int expectedArgCount = 1;
                if (arguments.Length != expectedArgCount)
                    return new Evaluation(
                        EvalCode.InvalidArgumentCount, new UnionValue(expectedArgCount));

                return new Evaluation(new UnionValue(Math.Atan(firstArg.Double) * 57.2957795));
            }
            else if (name.Span.SequenceEqual("lg"))
            {
                if (arguments.Length != 1)
                    return new Evaluation(EvalCode.InvalidArgumentCount);

                if (arguments[0].Child == null)
                    return new Evaluation(EvalCode.InvalidArguments);

                return new Evaluation(new UnionValue(Math.Log10(firstArg.Double)));
            }
            else if (name.Span.SequenceEqual("round"))
            {
                if (arguments.Length != 1 && arguments.Length != 2)
                    return new Evaluation(EvalCode.InvalidArgumentCount);

                if (arguments[0].Child == null ||
                    secondArg.Double < 0 || secondArg.Double > 15)
                    return new Evaluation(EvalCode.InvalidArguments);

                var rounded = Math.Round(firstArg.Double, (int)Math.Floor(secondArg.Double));
                return new Evaluation(new UnionValue(rounded));
            }
            else if (name.Span.SequenceEqual("length"))
            {
                if (arguments.Length == 0)
                    return new Evaluation(EvalCode.InvalidArgumentCount);

                var values = arguments;

                // TODO: add destructure operator for functions
                if (arguments[0].Children != null)
                {
                    if (arguments.Length > 1)
                        return new Evaluation(EvalCode.InvalidArguments);
                    values = arguments[0].Children;
                }

                double sum = 0;
                for (int i = 0; i < values.Length; i++)
                {
                    var child = values[i].Child;
                    if (!child.HasValue)
                        return new Evaluation(EvalCode.InvalidArguments);

                    sum += Math.Pow(child.Value.Double, 2);
                }

                return new Evaluation(new UnionValue(Math.Sqrt(sum)));
            }
            else if (name.Span.SequenceEqual("sqrt"))
            {
                int expectedArgCount = 1;
                if (arguments.Length != expectedArgCount)
                    return new Evaluation(
                        EvalCode.InvalidArgumentCount, new UnionValue(expectedArgCount));

                double input = firstArg.Double;
                if (input < 0)
                    return new Evaluation(EvalCode.InvalidArguments);

                double result = Math.Sqrt(input);
                return new Evaluation(new UnionValue(result));
            }
            return new Evaluation(EvalCode.UnresolvedFunction, name);
        }

        public static Evaluation ResolveOperator(
            ReadOnlyMemory<char> name, UnionValueCollection? left, UnionValueCollection? right)
        {
            if (name.Length != 1)
                return new Evaluation(EvalCode.UnresolvedOperator, name);

            var firstLeft = (left?.Child?.Double).GetValueOrDefault();
            var firstRight = (right?.Child?.Double).GetValueOrDefault();

            switch (name.Span[0])
            {
                case '+': return firstLeft + firstRight;

                case '-':
                case '–':
                    return firstLeft - firstRight;

                case '*': return firstLeft * firstRight;

                case ':':
                case '/': return firstLeft / firstRight;

                case '%': return firstLeft % firstRight;

                case '^': return Math.Pow(firstLeft, firstRight);

                case '!':
                    double number = firstLeft;
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
            if (args.PropertyName == nameof(ResultEvaluation) ||
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
            if (e.Key == Key.Enter)
            {
                Keyboard.ClearFocus();
                ValidateName();
            }
        }

        private void NameBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ValidateName();
        }

        private void ValidateName()
        {
            if (IsValidName(VariableName, out ReadOnlyString newName))
            {
                if (_lastVariableName != null)
                    MainWindow.GlobalExpressions.Remove(_lastVariableName);

                if (!newName.IsEmpty)
                    MainWindow.GlobalExpressions.Add(newName, this);

                var newNameString = newName.ToString();
                _lastVariableName = newNameString;
                
                VariableName = newNameString;
                InvokePropertyChanged(nameof(VariableName));

                foreach (var globalBox in MainWindow.GlobalExpressions.Values)
                    globalBox.UpdateResultValue();
            }
            else
            {
                VariableName = _lastVariableName;
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
