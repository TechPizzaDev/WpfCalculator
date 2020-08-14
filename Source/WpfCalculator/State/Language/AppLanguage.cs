using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WpfCalculator
{
    [JsonObject]
    public partial class AppLanguage
    {
        public const string FileExtension = ".json";

        public string EnglishName { get; set; }
        public string LocalName { get; set; }
        public EntryList Entries { get; }

        [JsonIgnore]
        public string? ResourceKey { get; set; }

        [JsonIgnore]
        public string? CultureKey { get; set; }

        [JsonConstructor]
        public AppLanguage(string englishName, string localName, EntryList entries)
        {
            EnglishName = englishName ?? throw new ArgumentNullException(nameof(englishName));
            LocalName = localName ?? throw new ArgumentNullException(nameof(localName));
            Entries = entries ?? throw new ArgumentNullException(nameof(entries));
        }

        protected virtual void SetKeys(string resourceKey)
        {
            ResourceKey = resourceKey;
            CultureKey = Path.GetFileNameWithoutExtension(resourceKey);
        }

        public static AppLanguage Load(string resourceKey, Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);

            var languageObject = App.Serializer.Deserialize<JObject>(stream);
            if (languageObject == null)
                throw new InvalidDataException("Null language object.");

            JToken? entries = languageObject[nameof(entries)];
            if (entries == null)
                throw new InvalidDataException("Missing entries.");
            
            var entryList = new EntryList(nameof(entries));

            var language = new AppLanguage(
                languageObject["englishName"]?.ToObject<string>() ?? "",
                languageObject["localName"]?.ToObject<string>() ?? "",
                entryList);

            PopulateEntryList(entries, entryList);

            language.SetKeys(resourceKey);
            return language;
        }

        public static IEnumerable<KeyValuePair<string, Stream>> GetEmbeddedLanguages(
            Assembly resourceAssembly)
        {
            using var reader = ResourceHelper.GetResourceReader(resourceAssembly);
            var enumerator = reader.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var entry = enumerator.Entry;
                if (entry.Key is string key &&
                    key.StartsWith(App.LanguagePath, StringComparison.OrdinalIgnoreCase) &&
                    key.EndsWith(FileExtension))
                {
                    if (entry.Value is Stream stream)
                        yield return new KeyValuePair<string, Stream>(key, stream);
                }
            }
        }
    }
}
