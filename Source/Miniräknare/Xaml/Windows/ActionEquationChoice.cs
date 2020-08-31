using System;

namespace Miniräknare
{
    public class ActionEquationChoice
    {
        public string Key { get; }
        public string[] Segments { get; }
        public AppLanguage.Entry Value { get; }

        public ActionEquationChoice(string key, string[] segments, AppLanguage.Entry value
            )
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Segments = segments ?? throw new ArgumentNullException(nameof(segments));
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}
