using System;
using System.IO;

namespace WpfCalculator
{
    public class AppLanguageEntry
    {
        private WeakReference<AppLanguage> _languageRef;

        public string Key { get; }
        public Stream Stream { get; }
        public bool IsVisible { get; set; }

        public AppLanguage Language => GetLanguage();

        public AppLanguageEntry(string key, Stream stream)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
            IsVisible = true;

            _languageRef = new WeakReference<AppLanguage>(null!);
        }

        public AppLanguage GetLanguage()
        {
            if (_languageRef.TryGetTarget(out var language))
                return language;

            language = AppLanguage.Load(Key, Stream);
            _languageRef.SetTarget(language);
            return language;
        }

        public void UnloadLanguage()
        {
            _languageRef.SetTarget(null!);
        }
    }
}
