using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Newtonsoft.Json.Linq;
using WpfCalculator.Expressions;
using WpfCalculator.Expressions.Tokens;

namespace WpfCalculator
{
    [DefaultBindingProperty(nameof(TextValue))]
    public partial class ExpressionBox : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string? _lastVariableName;
        private ExpressionTree _expressionTree;
        private ExpressionTreeProbe _probe;
        private ExpressionTreeEvaluator _evaluator;
        private bool _showName;
        private Evaluation _textParseEval = Evaluation.Empty;
        private Evaluation _expressionEval = Evaluation.Empty;
        private Evaluation _currentEval = Evaluation.Empty;

        public AppState State { get; }
        public HashSet<ExpressionBox> References { get; } = new HashSet<ExpressionBox>();

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

        public Evaluation CurrentEvaluation
        {
            get => _currentEval;
            set
            {
                _currentEval = value;
                InvokePropertyChanged();

                InvokePropertyChanged(nameof(Result));
                InvokePropertyChanged(nameof(Error));
            }
        }

        public JToken? Result => _currentEval.Result;
        public EError? Error => _currentEval.Error;

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
                    Evaluate();
                }
            }
        }

        #region Constructors

        public ExpressionBox()
        {
            InitializeComponent();
            State = App.Instance.StateProvider.State ??
                throw new InvalidOperationException("App instance is missing state provider.");

            ValueBox.TextChanged += ValueBox_TextChanged;
            IsVariableNameEnabled = true;
            ShowName = true;

            _expressionTree = new ExpressionTree(ExpressionOptions.Default);
            _evaluator = new ExpressionTreeEvaluator(
                ResolveReference,
                ResolveOperator,
                ResolveFunction);

            _probe = new ExpressionTreeProbe();
            _probe.ProbeReference += ProbeReference;
        }

        #endregion

        private void ValueBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            InvokePropertyChanged(nameof(TextValue));

            _textParseEval = ParseTextValue();

            Evaluate();
        }

        public Evaluation ParseTextValue()
        {
            if (ExpressionTree == null)
                return new EError(EErrorCode.Undefined);

            ExpressionTree.Tokens.Clear();
            ExpressionTokenizer.Tokenize(TextValue.AsMemory(), ExpressionTree.Tokens);

            var sanitizeResult = ExpressionSanitizer.Sanitize(ExpressionTree);
            if (sanitizeResult.Code != ExpressionSanitizer.ResultCode.Ok)
                return new EError(EErrorCode.SyntaxError);

            var parseCode = ExpressionParser.Parse(ExpressionTree);
            switch (parseCode)
            {
                case ExpressionParser.ResultCode.Ok:
                case ExpressionParser.ResultCode.NoTokens:
                    return new Evaluation(true);

                default:
                    // TODO: add more exact error object
                    return new EError(EErrorCode.SyntaxError);
            }
        }

        private void ClearReferences()
        {
            foreach (var field in References)
                field.PropertyChanged -= ReferenceChanged;
            References.Clear();
        }

        public Evaluation EvaluateExpressionTree()
        {
            if (ExpressionTree == null)
                return new Evaluation(EErrorCode.Empty);

            ClearReferences();
            _probe.Probe(ExpressionTree);

            var cyclicRefEval = CheckForCyclicReferences(VariableName, References);
            if (cyclicRefEval != null)
            {
                ClearReferences();
                return cyclicRefEval;
            }

            var eval = _evaluator.EvaluateTree(ExpressionTree);
            return eval;
        }

        private static Evaluation? CheckForCyclicReferences(
            string baseName, IEnumerable<ExpressionBox> baseReferences)
        {
            var checkedSet = new HashSet<ExpressionBox>();

            Evaluation? Core(bool isBase, string name, IEnumerable<ExpressionBox> references)
            {
                foreach (var reference in references)
                {
                    if (name == reference.VariableName)
                    {
                        var error = new EError(EErrorCode.CyclicReference).SetName(name);
                        return isBase ? error : new EError(EErrorCode.ErroredReference, error).SetName(name);
                    }

                    if (checkedSet.Add(reference))
                    {
                        var eval = Core(isBase: false, name, reference.References);
                        if (eval != null)
                            return eval;
                    }
                }
                return null;
            }

            var initialEval = Core(isBase: true, baseName, baseReferences);
            if (initialEval != null)
                return initialEval;

            foreach (var box in checkedSet)
            {
                var boxError = box.Error;
                if (boxError.ContainsError(EErrorCode.CyclicReference))
                    return new EError(EErrorCode.ErroredReference, boxError).SetName(box.VariableName);
            }
            return null;
        }

        private void ProbeReference(ValueToken name)
        {
            if (!State.Expressions.TryGetValue(name.Value, out var expression))
                return;

            if (References.Add(expression))
                expression.PropertyChanged += ReferenceChanged;
        }

        public void Evaluate(bool isReevaluatingState = false)
        {
            Evaluation nextEval;

            if (_textParseEval.Error != null)
            {
                nextEval = _textParseEval;
            }
            else
            {
                _expressionEval = EvaluateExpressionTree();
                nextEval = _expressionEval;
            }

            var lastEval = _currentEval;
            CurrentEvaluation = nextEval;

            if (lastEval.Error != null)
            {
                if (nextEval.Error == null)
                {
                    var resolvedError = lastEval.Error;
                    Console.WriteLine(VariableName + ": error resolved: " + resolvedError.Id);

                    if (!isReevaluatingState)
                        EvaluateExpressionsIfCyclicReference(resolvedError);
                }
                else
                {
                    var lastError = lastEval.Error;

                    Console.WriteLine(
                        VariableName + ": last/next error: " + lastError.Id + "/" + nextEval.Error.Id);

                    if (!isReevaluatingState)
                        EvaluateExpressionsIfCyclicReference(lastError);
                }
            }
            else
            {
                if (nextEval.Error != null)
                {
                    Console.WriteLine(VariableName + ": new error: " + nextEval.Error.Id);
                }
            }
        }

        private void EvaluateExpressionsIfCyclicReference(EError error)
        {
            // Cyclic references are cleared when detected,
            // so we have to notify expressions.
            if (error.ContainsError(EErrorCode.CyclicReference))
                State.EvaluateErroredExpressions();
        }

        private Evaluation ResolveReference(string name)
        {
            if (!State.Expressions.TryGetValue(name, out var field))
                return new EError(EErrorCode.UnknownReference).SetName(name);

            if (field.Error != null)
                return new EError(EErrorCode.ErroredReference, field.Error).SetName(name);

            return field.CurrentEvaluation;
        }

        private Evaluation ResolveFunction(string name, JToken?[] arguments)
        {
            var arg1 = arguments.Length > 0 ? arguments[0] : default;
            var arg2 = arguments.Length > 1 ? arguments[1] : default;

            if (name.Equals("sin", StringComparison.OrdinalIgnoreCase))
            {
                int expectedArgCount = 1;
                if (arguments.Length != expectedArgCount)
                {
                    var error = new EError(EErrorCode.InvalidArgumentCount);
                    error.Data["MinCount"] = expectedArgCount;
                    error.Data["MaxCount"] = expectedArgCount;
                    error.Data["Count"] = arguments.Length;
                    return error;
                }

                if (!arg1.IsNumber())
                {
                    var error = new EError(EErrorCode.InvalidArguments);
                    error.Data["Signature"] = "float"; // TODO: create signature object
                    return error;
                }

                return new Evaluation(Math.Sin(arg1.Value<double>() * 0.0174532925));
            }

            /*
            else if (name.Span.SequenceEqual("cos"))
            {
                int expectedArgCount = 1;
                if (arguments.Length != expectedArgCount)
                    return new Evaluation(
                        EvalErrorCode.InvalidArgumentCount, new UnionValue(expectedArgCount));

                return new Evaluation(new UnionValue(Math.Cos(arg1.Double * 0.0174532925)));
            }
            else if (name.Span.SequenceEqual("tan"))
            {
                int expectedArgCount = 1;
                if (arguments.Length != expectedArgCount)
                    return new Evaluation(
                        EvalErrorCode.InvalidArgumentCount, new UnionValue(expectedArgCount));

                return new Evaluation(new UnionValue(Math.Tan(arg1.Double) * 57.2957795));
            }
            if (name.Span.SequenceEqual("asin") || name.Span.SequenceEqual("arcsin"))
            {
                int expectedArgCount = 1;
                if (arguments.Length != expectedArgCount)
                    return new Evaluation(
                        EvalErrorCode.InvalidArgumentCount, new UnionValue(expectedArgCount));

                return new Evaluation(new UnionValue(Math.Asin(arg1.Double) * 57.2957795));
            }
            else if (name.Span.SequenceEqual("acos") || name.Span.SequenceEqual("arccos"))
            {
                int expectedArgCount = 1;
                if (arguments.Length != expectedArgCount)
                    return new Evaluation(
                        EvalErrorCode.InvalidArgumentCount, new UnionValue(expectedArgCount));

                return new Evaluation(new UnionValue(Math.Acos(arg1.Double) * 57.2957795));
            }
            else if (name.Span.SequenceEqual("atan") || name.Span.SequenceEqual("arctan"))
            {
                int expectedArgCount = 1;
                if (arguments.Length != expectedArgCount)
                    return new Evaluation(
                        EvalErrorCode.InvalidArgumentCount, new UnionValue(expectedArgCount));

                return new Evaluation(new UnionValue(Math.Atan(arg1.Double) * 57.2957795));
            }
            else if (name.Span.SequenceEqual("lg"))
            {
                if (arguments.Length != 1)
                    return new Evaluation(EvalErrorCode.InvalidArgumentCount);

                if (arguments[0].Child == null)
                    return new Evaluation(EvalErrorCode.InvalidArguments);

                return new Evaluation(new UnionValue(Math.Log10(arg1.Double)));
            }
            else if (name.Span.SequenceEqual("round"))
            {
                if (arguments.Length != 1 && arguments.Length != 2)
                    return new Evaluation(EvalErrorCode.InvalidArgumentCount);

                if (arguments[0].Child == null ||
                    arg2.Double < 0 || arg2.Double > 15)
                    return new Evaluation(EvalErrorCode.InvalidArguments);

                var rounded = Math.Round(arg1.Double, (int)Math.Floor(arg2.Double));
                return new Evaluation(new UnionValue(rounded));
            }
            else if (name.Span.SequenceEqual("length"))
            {
                if (arguments.Length == 0)
                    return new Evaluation(EvalErrorCode.InvalidArgumentCount);

                var values = arguments;

                // TODO: add destructure operator for functions
                if (arguments[0].Children != null)
                {
                    if (arguments.Length > 1)
                        return new Evaluation(EvalErrorCode.InvalidArguments);
                    values = arguments[0].Children;
                }

                double sum = 0;
                for (int i = 0; i < values.Length; i++)
                {
                    var child = values[i].Child;
                    if (!child.HasValue)
                        return new Evaluation(EvalErrorCode.InvalidArguments);

                    sum += Math.Pow(child.Value.Double, 2);
                }

                return new Evaluation(new UnionValue(Math.Sqrt(sum)));
            }
            else if (name.Span.SequenceEqual("sqrt"))
            {
                int expectedArgCount = 1;
                if (arguments.Length != expectedArgCount)
                    return new Evaluation(
                        EvalErrorCode.InvalidArgumentCount, new UnionValue(expectedArgCount));

                double input = arg1.Double;
                if (input < 0)
                    return new Evaluation(EvalErrorCode.InvalidArguments);

                double result = Math.Sqrt(input);
                return new Evaluation(new UnionValue(result));
            }*/

            return new EError(EErrorCode.UnknownFunction).SetName(name);
        }

        public static Evaluation ResolveOperator(string name, JToken? left, JToken? right)
        {
            if (name.Length != 1)
                return new EError(EErrorCode.UnknownOperator).SetName(name);

            var left1 = (left?.Value<double>()).GetValueOrDefault();
            var right1 = (right?.Value<double>()).GetValueOrDefault();

            switch (name[0])
            {
                case '+':
                    return left1 + right1;

                case '-':
                case '–':
                    return left1 - right1;

                case '*':
                    return left1 * right1;

                case ':':
                case '/':
                    return left1 / right1;

                case '%':
                    return left1 % right1;

                case '^':
                    return Math.Pow(left1, right1);

                case '!':
                    double number = left1;
                    double result = 1;
                    while (number != 1)
                    {
                        result *= number;
                        number -= 1;
                    }
                    return result;

                default:
                    return new EError(EErrorCode.UnknownOperator).SetName(name);
            }
        }

        private void ReferenceChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == nameof(Result) ||
                args.PropertyName == nameof(Error))
            {
                Evaluate();
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
            if (State.IsValidName(VariableName, out ReadOnlyString newName))
            {
                if (_lastVariableName != null)
                    State.Expressions.Remove(_lastVariableName);

                if (!newName.IsEmpty)
                    State.Expressions.Add(newName, this);

                var newNameString = newName.ToString();
                _lastVariableName = newNameString;

                VariableName = newNameString;
                InvokePropertyChanged(nameof(VariableName));

                State.EvaluateErroredExpressions();
            }
            else
            {
                if (_lastVariableName != null)
                    VariableName = _lastVariableName;
            }
        }

        private void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            // TODO: implement this
            return;

            bool isText = e.SourceDataObject.GetDataPresent(DataFormats.Html, true);
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
