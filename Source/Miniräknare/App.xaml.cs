using System.Windows;
using System.Linq;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;

namespace Miniräknare
{
    public partial class App : Application
    {
        public const string LanguageProviderKey = "LanguageProvider";
        public const string FallbackLanguage = "en-US.xml";

        public const string ScriptPath = "Content/Scripts";
        public const string LanguagePath = "Content/Language";

        private SplashScreenWindow _splashScreen;

        private AppLanguageProvider _languageProvider;

        public static App Instance { get; private set; }

        public App()
        {
            Instance = this;

            _splashScreen = new SplashScreenWindow();
            MainWindow = _splashScreen;
            _splashScreen.Show();
        }

        #region Startup

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Task.Run(() =>
            {
                InitializeLanguageProvider();
                _splashScreen.DispatchProgress(5);

                MathScriptFactory.Initialize();
                _splashScreen.DispatchProgress(10);

                _splashScreen.DispatchProgressTip(_languageProvider.GetValue("Other/Loading/InternalScripts"));
                LoadScripts((x) => _splashScreen.DispatchProgress(10 + x * 90));

                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);

                Dispatcher.Invoke(() =>
                {
                    _splashScreen.ProgressTip = null;

                    var mainWindow = new MainWindow();
                    MainWindow = mainWindow;
                    mainWindow.Show();
                    _splashScreen.Close();
                });
            });
        }

        #endregion

        #region Scripts

        private void LoadScripts(Action<double> onProgress)
        {
            using var resourceReader = ResourceHelper.GetResourceReader(ResourceAssembly);
            var enumerator = resourceReader.GetEnumerator();
            var pairs = new List<KeyValuePair<string, Stream>>();
            while (enumerator.MoveNext())
            {
                var entry = enumerator.Entry;
                if (entry.Key is string key &&
                    key.StartsWith(ScriptPath, StringComparison.OrdinalIgnoreCase) &&
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

                    //var scriptData = MathScriptData.Load(stream);
                    //var script = scriptData.Compile(tmpStringBuilder, generateSymbols: false);
                    
                    //var stringWrtier = new StringWriter();
                    //MathScriptData.Serializer.Serialize(stringWrtier, scriptData);
                    //Console.WriteLine(stringWrtier.ToString());
                }
                catch(Exception ex)
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
                (pair) => pair.Key.EndsWith("sv-SE.xml", StringComparison.OrdinalIgnoreCase)).First();

            // TODO: load main language based on settings file

            _languageProvider.Language = AppLanguage.Load(langEntry.Key, langEntry.Value);
            _languageProvider.FallbackLanguage = AppLanguage.Load(fallbackLangEntry.Key, fallbackLangEntry.Value);
        }

        #endregion
    }
}
