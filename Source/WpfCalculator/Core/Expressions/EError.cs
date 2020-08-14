using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WpfCalculator.Expressions;

namespace WpfCalculator
{
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ", nq}")]
    [JsonObject]
    public class EError
    {
        public static EError Empty { get; } = new EError(EErrorCode.Empty);

        public string Id { get; }
        public Dictionary<string, JToken> Data { get; }
        public EError? InnerError { get; }

        private string JsonView => ToJson(Formatting.Indented);

        internal string DebuggerDisplay => $"{nameof(EError)}<{Id}>";

        public JToken this[string dataKey] => Data[dataKey];

        [JsonConstructor]
        public EError(string id, EError? innerError = null, Dictionary<string, JToken>? data = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Data = data ?? new Dictionary<string, JToken>();
            InnerError = innerError;
        }

        public EError(EErrorCode code, EError? innerError = null, Dictionary<string, JToken>? data = null) :
            this(code.ToString(), innerError, data)
        {
        }

        public string ToJson(Formatting formatting = Formatting.None)
        {
            return JsonConvert.SerializeObject(this, formatting, JsonHelper.IgnoreNullSettings);
        }

        public static implicit operator EError(EErrorCode code)
        {
            return new EError(code);
        }
    }
}
