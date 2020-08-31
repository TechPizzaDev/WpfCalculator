using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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

        public const string ContentPath = "Content";
        public const string EquationsPath = ContentPath + "/Equations";
        public const string LanguagePath = ContentPath + "/Language";

        public static ResourceUri LoadingEquationsMessage { get; } =
            new ResourceUri("Other/Loading/CoreEquations");

        public static JsonSerializer Serializer { get; } = new JsonSerializer()
        {
            Formatting = Formatting.Indented
        };

        public static App Instance { get; private set; }

        private SplashScreenWindow _splashScreen;
        private AppLanguageProvider _languageProvider;

        public Dictionary<string, EquationSet> Equations { get; } =
            new Dictionary<string, EquationSet>();

        public App()
        {
            Instance = this;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            _splashScreen = new SplashScreenWindow();
            MainWindow = _splashScreen;
            _splashScreen.Show();

            //TestExpressionLoop();
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

                            var evaluator = new ExpressionTreeEvaluator(
                                ResolveReference,
                                ExpressionBox.ResolveOperator,
                                ResolveFunction);

                            var eval = Evaluation.Undefined;
                            for (int i = 0; i < 1; i++)
                                eval = evaluator.EvaluateTree(tree);

                            if (eval.Code != EvalCode.Ok)
                            {
                                Console.WriteLine("Eval code: " + eval.Code);
                            }
                            else
                            {
                                double evalValue = (double)eval.Values.Child?.Double;
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

                    _splashScreen.DispatchProgressTip(_languageProvider.GetValue(LoadingEquationsMessage));
                    LoadEquations((x) => _splashScreen.DispatchProgress(10 + x * 90));

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

        #region Equations

        private void LoadEquations(Action<double> onProgress)
        {
            using var resourceReader = ResourceHelper.GetResourceReader(ResourceAssembly);
            var enumerator = resourceReader.GetEnumerator();
            var pairs = new List<KeyValuePair<string, Stream>>();
            while (enumerator.MoveNext())
            {
                var entry = enumerator.Entry;
                if (entry.Key is string key &&
                    entry.Value is Stream stream &&
                    key.StartsWith(EquationsPath, StringComparison.OrdinalIgnoreCase))
                {
                    pairs.Add(new KeyValuePair<string, Stream>(key, stream));
                }
            }

            var tmpBuilder = new StringBuilder();
            var tmpTokenList = new List<Token>();
            for (int i = 0; i < pairs.Count; i++)
            {
                try
                {
                    var pair = pairs[i];
                    var stream = pair.Value;

                    stream.Seek(0, SeekOrigin.Begin);

                    var equationData = EquationSetData.Load(stream);
                    var equationSet = new EquationSet(ExpressionOptions.Default, equationData);

                    Equations.Add(pair.Key, equationSet);
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
