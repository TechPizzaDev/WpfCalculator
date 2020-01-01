using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;

namespace Miniräknare
{
    public class AppLanguage
    {
        public static XmlSerializer Serializer { get; } = new XmlSerializer(typeof(AppLanguage));

        private Entry[] _entries;
        private Dictionary<string, Entry> _entryMap;

        public string EnglishName { get; set; }
        public string LocalName { get; set; }

        [XmlIgnore]
        public string ResourceKey { get; set; }

        [XmlIgnore]
        public string CultureKey { get; set; }

        public Entry[] Entries
        {
            get => _entries;
            set
            {
                if (_entries == value)
                    return;
                _entries = value;

                _entryMap.Clear();
                if (_entries != null)
                    foreach (var entry in _entries)
                        _entryMap.Add(entry.Key, entry);
            }
        }

        [XmlIgnore]
        public ReadOnlyDictionary<string, Entry> EntryMap { get;  }

        public AppLanguage()
        {
            _entryMap = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
            EntryMap = new ReadOnlyDictionary<string, Entry>(_entryMap);
        }

        protected virtual void SetBaseValues(string key)
        {
            ResourceKey = key;
            CultureKey = Path.GetFileNameWithoutExtension(key);
        }

        public static AppLanguage Load(string key, Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            var language = Serializer.Deserialize(stream) as AppLanguage;
            language.SetBaseValues(key);
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
                    key.StartsWith(App.LanguagePath, StringComparison.OrdinalIgnoreCase))
                {
                    var stream = entry.Value as Stream;
                    yield return new KeyValuePair<string, Stream>(key, stream);
                }
            }
        }

        public class Entry
        {
            [XmlAttribute]
            public string Key { get; set; }

            [XmlText]
            public string Value { get; set; }
        }
    }
}
