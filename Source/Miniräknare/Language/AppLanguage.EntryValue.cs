using System;
using System.Diagnostics;

namespace Miniräknare
{
    public partial class AppLanguage
    {
        [DebuggerDisplay("{Key}: {Value}")]
        public class EntryValue : Entry
        {
            public object Value { get; }

            public EntryValue(string key, object value) : base(key)
            {
                Value = value ?? throw new ArgumentNullException(nameof(value));
            }
        }
    }
}
