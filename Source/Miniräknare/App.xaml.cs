using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Miniräknare.Expressions;
using Miniräknare.Expressions.Tokens;
using Newtonsoft.Json;

namespace Miniräknare
{
    public partial class App : Application
    {
        public const string LanguageProviderKey = "LanguageProvider";
        public const string FallbackLanguage = "en-US.json";
        public static readonly ResourceUri LoadingFormulasMessage = new ResourceUri("Other/Loading/InternalFormulas");

        public const string FormulasPath = "Content/Formulas";
        public const string LanguagePath = "Content/Language";

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


            //TestExpressionLoop();
        }

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

                var result = ExpressionSanitizer.Sanitize(tree);
                int index = result.ErrorTokenPosition ?? -1;
                //Console.WriteLine("SanitizeTokens code: " + result.Code + " at index " + index);

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
                            var evaluator = new ExpressionTreeEvaluator(
                                tree,
                                ResolveReference,
                                ResolveOperator,
                                ResolveFunction);

                            var eval = Evaluation.Undefined;
                            for (int i = 0; i < 1; i++)
                                eval = evaluator.Evaluate();

                            if (eval.Code != EvalCode.Ok)
                            {
                                Console.WriteLine("Eval code: " + eval.Code);
                            }
                            else
                            {
                                double evalValue = eval.Value.Double;
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

        public static Evaluation ResolveFunction(ReadOnlyMemory<char> name, ReadOnlySpan<UnionValue> arguments)
        {
            return new Evaluation(EvalCode.UnresolvedFunction);
        }

        public static Evaluation ResolveOperator(ReadOnlyMemory<char> name, UnionValue? left, UnionValue right)
        {
            if (name.Length != 1)
                return new Evaluation(EvalCode.UnresolvedOperator);

            switch (name.Span[0])
            {
                case '+': return left.GetValueOrDefault().Double + right.Double;
                case '-': return left.GetValueOrDefault().Double - right.Double;

                case '*': return left.GetValueOrDefault().Double * right.Double;
                case '/': return left.GetValueOrDefault().Double / right.Double;
                    
                case '%': return left.GetValueOrDefault().Double % right.Double;

                case '^': return Math.Pow(left.Value.Double, right.Double);

                default:
                    return new Evaluation(EvalCode.UnresolvedOperator);
            }
        }

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
            for (int i = 0; i < pairs.Count; i++)
            {
                try
                {
                    var pair = pairs[i];
                    var stream = pair.Value;

                    var formulaData = MathFormulaData.Load(stream);
                    var formula = new MathFormula(formulaData);


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
