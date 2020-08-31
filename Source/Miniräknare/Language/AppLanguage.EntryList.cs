using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Miniräknare
{
    public partial class AppLanguage
    {
        private static void PopulateEntryList(JToken token, EntryList list)
        {
            foreach (var property in token.Select(x => (JProperty)x))
            {
                string key = property.Name;
                var value = property.Value;
                if (value is JObject valueObject)
                {
                    if (key == "_names")
                    {
                        foreach (var nameToken in property.Value)
                        {
                            var nameProp = (JProperty)nameToken;
                            var nameEntry = new EntryValue(nameProp.Name, nameProp.Value.ToObject<string>());
                            list.Names.Add(nameEntry.Key, nameEntry);
                        }
                    }
                    else
                    {
                        var subList = new EntryList(key);
                        list.SubLists.Add(key, subList);
                        PopulateEntryList(valueObject, subList);
                    }
                }
                else
                {
                    list.Values.Add(key, new EntryValue(key, value.ToObject<string>()));
                }
            }

            foreach (var name in list.Names)
            {
                if (list.SubLists.TryGetValue(name.Key, out var subList))
                {
                    subList.Name = name.Value;
                }
            }
        }

        public class EntryList : Entry
        {
            public EntryValue Name { get; set; }
            public Dictionary<string, EntryValue> Names { get; }
            public Dictionary<string, EntryValue> Values { get; }
            public Dictionary<string, EntryList> SubLists { get; }

            public EntryList(string key) : base(key)
            {
                Names = new Dictionary<string, EntryValue>(StringComparer.OrdinalIgnoreCase);
                Values = new Dictionary<string, EntryValue>(StringComparer.OrdinalIgnoreCase);
                SubLists = new Dictionary<string, EntryList>(StringComparer.OrdinalIgnoreCase);
            }

            public bool TryGet(ResourceUri uri, out Entry entry)
            {
                static bool RecursiveGet(EntryList list, ResourceUri uri, int segmentIndex, out Entry entry)
                {
                    string segment = uri.Segments[segmentIndex];

                    // Only try to get values when we're at the last segment.
                    if (segmentIndex == uri.Segments.Length - 1)
                    {
                        if (list.Names.TryGetValue(segment, out var nameEntry))
                        {
                            entry = nameEntry;
                            return true;
                        }
                        if (list.Values.TryGetValue(segment, out var valueEntry))
                        {
                            entry = valueEntry;
                            return true;
                        }
                    }

                    // If getting a value fails, we fallback and try to return a list.
                    if (list.SubLists.TryGetValue(segment, out var listEntry))
                    {
                        if (segmentIndex + 1 >= uri.Segments.Length)
                        {
                            entry = null;
                            return false;
                        }
                        return RecursiveGet(listEntry, uri, segmentIndex + 1, out entry);
                    }

                    entry = default;
                    return false;
                }

                return RecursiveGet(this, uri, segmentIndex: 0, out entry);
            }
        }
    }
}
