using System;

namespace WpfCalculator
{
    public partial class AppLanguage
    {
        public abstract class Entry
        {
            public string Key { get; }

            public Entry(string key)
            {
                Key = key ?? throw new ArgumentNullException(nameof(key));
            }
        }
    }
}
