using System.ComponentModel;

namespace Miniräknare
{
    public class AppLanguageProvider : INotifyPropertyChanged
    {
        private AppLanguage _fallbackLanguage;
        private AppLanguage _language;

        public event PropertyChangedEventHandler PropertyChanged;

        public AppLanguage FallbackLanguage
        {
            get => _fallbackLanguage;
            set
            {
                if (_fallbackLanguage != value)
                {
                    _fallbackLanguage = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language)));
                }
            }
        }

        public AppLanguage Language
        {
            get => _language;
            set
            {
                if (_language != value)
                {
                    _language = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language)));
                }
            }
        }

        public string GetValue(string key)
        {
            static int GetValue(AppLanguage language, string key, out string value)
            {
                if (language == null)
                {
                    value = "[No Language]";
                    return -1;
                }

                if (!language.EntryMap.TryGetValue(key, out var entry))
                {
                    value = $"[Missing '{key}']";
                    return -2;
                }

                if (entry.Value == null ||
                    (entry.Value is string text && string.IsNullOrWhiteSpace(text)))
                {
                    value = $"['{key}']";
                    return -3;
                }

                value = entry.Value;
                return 0;
            }

            int valueCode = GetValue(Language, key, out string value);
            if (valueCode < 0)
            {
                int fallbackValueCode = GetValue(FallbackLanguage, key, out string fallbackValue);
                if (valueCode == -1 || fallbackValueCode == 0)
                    value = fallbackValue;
            }
            return value;
        }
    }
}
