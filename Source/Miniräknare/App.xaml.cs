using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Miniräknare.Expressions;
using Miniräknare.Expressions.Tokens;
using Newtonsoft.Json;

namespace Miniräknare
{
    public partial class App : Application
    {
        public const string LanguageProviderKey = "LanguageProvider";
        public const string FallbackLanguage = "en-US.json";

        public const string FormulasPath = "Content/Formulas";
        public const string LanguagePath = "Content/Language";

        public static ResourceUri LoadingFormulasMessage { get; } =
            new ResourceUri("Other/Loading/InternalFormulas");

        private SplashScreenWindow _splashScreen;
        private AppLanguageProvider _languageProvider;

        public static App Instance { get; private set; }

        public static JsonSerializer Serializer { get; } = new JsonSerializer()
        {
            Formatting = Formatting.Indented
        };

        public App()
        {
            Instance = this;

            _splashScreen = new SplashScreenWindow();
            MainWindow = _splashScreen;
            _splashScreen.Show();

            TestExpressionLoop();
        }

        #region TestExpressionLoop

        private static void TestExpressionLoop()
        {
            static void Print(IEnumerable<Token> tt)
            {
                string x = "";
                foreach (var r in tt)
                    x += r.ToString();
                Console.WriteLine(x);
            }

            var tree = new ExpressionTree(ExpressionOptions.Default);

            while (true)
            {
                string input = Console.ReadLine();

                tree.Tokens.Clear();
                //ExpressionTokenizer.TokenizeInput(".2__5__5 + yo_u +1_0  - 5_99.1 + xD  () + wat(nou; 25; omg(45; a))".AsMemory(), tree.Tokens);
                ExpressionTokenizer.Tokenize(input.AsMemory(), tree.Tokens);
                //Print(tree.Tokens);

                // https://en.wikipedia.org/wiki/Shunting-yard_algorithm
                // awesome stuff
                var output = new Queue<Token>();
                var opStack = new Stack<Token>();
                var tokens = tree.Tokens;
                for (int i = 0; i < tokens.Count; i++)
                {
                    var token = tokens[i];
                    if (token.Type == TokenType.DecimalDigit)
                    {
                        output.Enqueue(token);
                    }
                    else if (token.Type == TokenType.Function)
                    {
                        opStack.Push(token);
                    }
                    else if (token.Type == TokenType.Operator)
                    {
                        bool TryPop()
                        {
                            if (opStack.Count == 0)
                                return false;

                            var peek = opStack.Peek();
                            if (peek.Type == TokenType.ListStart)
                                return false;

                            if (peek.Type == TokenType.Function)
                                return true;

                            var opToken = (ValueToken)token;
                            if (peek.Type == TokenType.Operator)
                            {
                                var peekOpToken = (ValueToken)peek;
                                var peekDef = tree.ExpressionOptions.GetOperatorDefinition(peekOpToken.Value);
                                var opDef = tree.ExpressionOptions.GetOperatorDefinition(opToken.Value);

                                if (peekDef.Precedence > opDef.Precedence)
                                    return true;

                                if (peekDef.Precedence == opDef.Precedence &&
                                    opDef.Associativity == OperatorAssociativity.Left)
                                    return true;
                            }
                            return false;
                        }

                        while (TryPop())
                        {
                            output.Enqueue(opStack.Pop());
                        }
                        opStack.Push(token);
                    }
                    else if (token.Type == TokenType.ListStart)
                    {
                        opStack.Push(token);
                    }
                    else if (token.Type == TokenType.ListEnd)
                    {
                        bool TryPop()
                        {
                            if (opStack.Count > 0)
                            {
                                var peek = opStack.Peek();
                                if (peek.Type != TokenType.ListStart)
                                    return true;
                            }
                            return false;
                        }

                        while (TryPop())
                            output.Enqueue(opStack.Pop());

                        // If the stack runs out without finding a left paren, then there are mismatched parentheses.
                        if (opStack.Count > 0 && opStack.Peek().Type == TokenType.ListStart)
                            opStack.Pop();
                        //else
                        // error;
                    }
                }

                // After while loop, if operator stack not null, pop everything to output queue 
                while (opStack.Count > 0)
                {
                    var popped = opStack.Pop();

                    // If the operator token on the top of the stack is a paren, then there are mismatched parentheses.
                    //if (popped.Type == TokenType.ListStart ||
                    //    popped.Type == TokenType.ListEnd)
                    //    // error

                    output.Enqueue(popped);
                }

                var evaluator = new ExpressionTreeEvaluator(
                    ResolveReference,
                    ExpressionBox.ResolveOperator,
                    ResolveFunction);


                // https://en.wikipedia.org/wiki/Reverse_Polish_notation
                // and simple evaluation too :D

                //var expressionStack = new Stack<UnionValueCollection>();
                //foreach (var token in output)
                //{
                //    if (token.Type == TokenType.Operator)
                //    {
                //        var op = (ValueToken)token;
                //        var operand2 = expressionStack.Pop();
                //        var operand1 = expressionStack.Pop();
                //        var evalResult = evaluator.ExecuteOperator(op.Value, operand1, operand2);
                //        expressionStack.Push(evalResult.Values);
                //    }
                //    else if (
                //        token.Type == TokenType.DecimalDigit || 
                //        token.Type == TokenType.DecimalNumber)
                //    {
                //        var eval = evaluator.EvaluateToken(tree.ExpressionOptions, token);
                //        expressionStack.Push(eval.Values);
                //    }
                //    else
                //    {
                //        Console.WriteLine("oof");
                //    }
                //}

                var expressionStack = new Stack<Token>();
                foreach (var token in output)
                {
                    if (token.Type == TokenType.Operator)
                    {
                        var operand2 = expressionStack.Pop();
                        var operand1 = expressionStack.Pop();
                        expressionStack.Push(new ListToken(null, new List<Token>(3) { operand1, token, operand2 }));
                    }
                    else if (
                        token.Type == TokenType.DecimalDigit || 
                        token.Type == TokenType.DecimalNumber)
                    {
                        expressionStack.Push(token);
                    }
                    else
                    {
                        Console.WriteLine("oof");
                    }
                }
                
                var eresult = expressionStack.Pop();

                var evale = evaluator.Evaluate(tree.ExpressionOptions, ((ListToken)eresult).Children);
                
                continue;

                var result = ExpressionSanitizer.Sanitize(tree);
                //Console.WriteLine("SanitizeTokens code: " + result.Code + ", ErrorToken: " + result.ErrorToken);

                if (result.Code == ExpressionSanitizer.ResultCode.Ok)
                {
                    //Print(tree.Tokens);
                    var parseCode = ExpressionParser.Parse(tree);
                    if (parseCode == ExpressionParser.ResultCode.Ok)
                    {
                        var optimizeCode = ExpressionReducer.Reduce(tree);
                        if (optimizeCode == ExpressionReducer.ResultCode.Ok)
                        {
                            Print(tree.Tokens);

                            var eval = Evaluation.Undefined;
                            for (int i = 0; i < 1; i++)
                                eval = evaluator.Evaluate(tree);

                            if (eval.Code != EvalCode.Ok)
                            {
                                Console.WriteLine("Eval code: " + eval.Code);
                            }
                            else
                            {
                                double evalValue = eval.Values.First.Double;
                                string textValue = double.IsInfinity(evalValue) ? "Infinity" : evalValue.ToString();
                                Console.WriteLine("Eval: " + textValue);
                            }
                        }
                        else
                            Console.WriteLine("Optimize code: " + parseCode);
                    }
                    else
                        Console.WriteLine("Parse code: " + parseCode);
                }
            }
        }

        public static Evaluation ResolveReference(ReadOnlyMemory<char> name)
        {
            return new Evaluation(EvalCode.UnresolvedReference);
            throw new NotImplementedException();
        }

        public static Evaluation ResolveFunction(
            ReadOnlyMemory<char> name, ReadOnlySpan<UnionValueCollection> arguments)
        {
            return new Evaluation(EvalCode.UnresolvedFunction);
        }

        #endregion

        #region Startup

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Task.Run(() =>
            {
                try
                {
                    InitializeLanguageProvider();
                    _splashScreen.DispatchProgress(10);

                    _splashScreen.DispatchProgressTip(_languageProvider.GetValue(LoadingFormulasMessage));
                    LoadFormulas((x) => _splashScreen.DispatchProgress(10 + x * 90));

                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);

                    Dispatcher.Invoke(() =>
                    {
                        _splashScreen.ProgressTip = null;

                        var mainWindow = new MainWindow();
                        MainWindow = mainWindow;
                        mainWindow.Show();
                        _splashScreen.Close();
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    Dispatcher.Invoke(() =>
                    {
                        Shutdown(-1);
                    });
                }
            });
        }

        #endregion

        #region Formulas

        private void LoadFormulas(Action<double> onProgress)
        {
            using var resourceReader = ResourceHelper.GetResourceReader(ResourceAssembly);
            var enumerator = resourceReader.GetEnumerator();
            var pairs = new List<KeyValuePair<string, Stream>>();
            while (enumerator.MoveNext())
            {
                var entry = enumerator.Entry;
                if (entry.Key is string key &&
                    key.StartsWith(FormulasPath, StringComparison.OrdinalIgnoreCase) &&
                    entry.Value is Stream stream)
                {
                    pairs.Add(new KeyValuePair<string, Stream>(key, stream));
                }
            }

            var tmpStringBuilder = new StringBuilder();
            var tmpTokenList = new List<Token>();
            for (int i = 0; i < pairs.Count; i++)
            {
                try
                {
                    var pair = pairs[i];
                    var stream = pair.Value;

                    var formulaData = MathFormulaData.Load(stream);
                    var formula = new MathFormula(ExpressionOptions.Default, formulaData);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                onProgress?.Invoke((i + 1.0) / pairs.Count);
            }
        }

        #endregion

        #region Language

        private void InitializeLanguageProvider()
        {
            _languageProvider = FindResource(LanguageProviderKey) as AppLanguageProvider;
            if (_languageProvider == null)
                throw new Exception("Could not find language provider.");

            var languages = AppLanguage.GetEmbeddedLanguages(ResourceAssembly);

            var fallbackLangEntry = languages.Where(
                (pair) => pair.Key.EndsWith(FallbackLanguage, StringComparison.OrdinalIgnoreCase)).First();

            var langEntry = languages.Where(
                (pair) => pair.Key.EndsWith("sv-SE.json", StringComparison.OrdinalIgnoreCase)).First();

            // TODO: load main language based on settings file

            _languageProvider.Language = AppLanguage.Load(langEntry.Key, langEntry.Value);
            _languageProvider.FallbackLanguage = AppLanguage.Load(fallbackLangEntry.Key, fallbackLangEntry.Value);
        }

        #endregion
    }
}
