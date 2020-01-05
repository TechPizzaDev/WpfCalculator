using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Miniräknare
{
    [JsonObject]
    public partial class AppLanguage
    {
        public const string FileExtension = ".json";

        public string EnglishName { get; set; }
        public string LocalName { get; set; }
        public EntryList Entries { get; }

        [JsonIgnore]
        public string ResourceKey { get; set; }

        [JsonIgnore]
        public string CultureKey { get; set; }

        [JsonConstructor]
        public AppLanguage(string englishName, string localName, EntryList entries)
        {
            EnglishName = englishName ?? throw new ArgumentNullException(nameof(englishName));
            LocalName = localName ?? throw new ArgumentNullException(nameof(localName));
            Entries = entries ?? throw new ArgumentNullException(nameof(entries));
        }

        protected virtual void SetKeys(string key)
        {
            ResourceKey = key;
            CultureKey = Path.GetFileNameWithoutExtension(key);
        }

        public static AppLanguage Load(string key, Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            var languageObject = App.Serializer.Deserialize<JObject>(stream);

            JToken entries = languageObject[nameof(entries)];
            var entryList = new EntryList(nameof(entries));

            var language = new AppLanguage(
                languageObject["englishName"].ToObject<string>(),
                languageObject["localName"].ToObject<string>(),
                entryList);

            PopulateEntryList(entries, entryList);

            language.SetKeys(key);
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
                    var stream = entry.Value as Stream;
                    yield return new KeyValuePair<string, Stream>(key, stream);
                }
            }
        }
    }
}
