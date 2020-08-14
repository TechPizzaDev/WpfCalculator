using System.ComponentModel;

namespace WpfCalculator
{
    public class AppLanguageProvider : INotifyPropertyChanged
    {
        private AppLanguage? _fallbackLanguage;
        private AppLanguage? _language;

        public event PropertyChangedEventHandler? PropertyChanged;

        public AppLanguage? FallbackLanguage
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

        public AppLanguage? Language
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

        public string GetValue(ResourceUri uri)
        {
            static int GetValue(AppLanguage? language, ResourceUri uri, out string value)
            {
                if (language == null)
                {
                    value = "[No Language]";
                    return -1;
                }

                if (!language.Entries.TryGet(uri, out var entry))
                {
                    value = $"[Missing '{uri}']";
                    return -2;
                }

                if (!(entry is AppLanguage.EntryValue entryValue))
                {
                    value = $"['{uri}' is invalid]";
                    return -3;
                }

                if (entryValue.Value == null ||
                    (entryValue.Value is string text && string.IsNullOrWhiteSpace(text)))
                {
                    value = $"['{uri}']";
                    return -3;
                }

                value = (string)entryValue.Value;
                return 0;
            }

            int valueCode = GetValue(Language, uri, out string value);
            if (valueCode < 0)
            {
                int fallbackValueCode = GetValue(FallbackLanguage, uri, out string fallbackValue);
                if (valueCode == -1 || fallbackValueCode == 0)
                    value = fallbackValue;
            }
            return value;
        }
    }
}
