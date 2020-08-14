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
using WpfCalculator.Expressions;
using WpfCalculator.Expressions.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WpfCalculator
{
    public partial class App : Application
    {
        public const string LatinAlphabet = "abcdefghijklmnopqrstuvwxyz";

        public const string LanguageProviderKey = "LanguageProvider";
        public const string StateProviderKey = "StateProvider";

        public const string EquationsPath = "Content/Equations";
        public const string LanguagePath = "Content/Language";

        public const string FallbackLanguage = "en-US.json";

        public static ResourceUri LoadingEquationsMessage { get; } =
            new ResourceUri("Other/Loading/CoreEquations");

        public static JsonSerializer Serializer { get; } = new JsonSerializer()
        {
            Formatting = Formatting.Indented
        };

        public static App Instance => (App)Current;

        private SplashScreenWindow _splashScreen;

        public AppLanguageProvider LanguageProvider { get; private set; }
        public AppStateProvider StateProvider { get; private set; }

        public App()
        {
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

                            var eval = new Evaluation(EErrorCode.Empty);
                            for (int i = 0; i < 1; i++)
                                eval = evaluator.EvaluateTree(tree);

                            if (eval.Error == null)
                            {
                                Console.WriteLine("Eval error: " + eval.Error);
                            }
                            else
                            {
                                double evalValue = (eval.Result?.Value<double>()).GetValueOrDefault();
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

        public static Evaluation ResolveReference(string name)
        {
            return new EError(EErrorCode.UnknownReference).SetName(name);
            throw new NotImplementedException();
        }

        public static Evaluation ResolveFunction(string name, object?[] arguments)
        {
            return new EError(EErrorCode.UnknownFunction).SetName(name);
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

                    _splashScreen.DispatchProgressTip(LanguageProvider.GetValue(LoadingEquationsMessage));
                    LoadEquations((x) => _splashScreen.DispatchProgress(10 + x * 90));

                    InitializeStateProvider();

                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);

                    Dispatcher.Invoke(() =>
                    {
                        _splashScreen.ProgressTip = string.Empty;

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

        public void InitializeStateProvider()
        {
            StateProvider = (AppStateProvider)FindResource(StateProviderKey);
            if (StateProvider == null)
                throw new Exception("Could not find state provider.");

            StateProvider.State = new AppState();
        }

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

                    // TODO
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
            LanguageProvider = (AppLanguageProvider)FindResource(LanguageProviderKey);
            if (LanguageProvider == null)
                throw new Exception("Could not find language provider.");

            var languages = AppLanguage.GetEmbeddedLanguages(ResourceAssembly);

            var fallbackLangEntry = languages.Where(
                (pair) => pair.Key.EndsWith(FallbackLanguage, StringComparison.OrdinalIgnoreCase)).First();

            var langEntry = languages.Where(
                (pair) => pair.Key.EndsWith("sv-SE.json", StringComparison.OrdinalIgnoreCase)).First();

            // TODO: load main language based on settings file

            LanguageProvider.Language = AppLanguage.Load(langEntry.Key, langEntry.Value);
            LanguageProvider.FallbackLanguage = AppLanguage.Load(fallbackLangEntry.Key, fallbackLangEntry.Value);
        }

        #endregion
    }
}
