using System;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WpfCalculator.Expressions
{
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ", nq}")]
    [JsonObject]
    public class Evaluation
    {
        public static Evaluation Empty { get; } = new Evaluation(EError.Empty);

        public JToken? Result { get; }
        public EError? Error { get; }

        private string JsonView => ToJson(Formatting.Indented);
        
        internal string DebuggerDisplay =>
            Error != null ? $"{nameof(Evaluation)}<{Error.Id}>" :
            Result != null ? $"{nameof(Evaluation)}({Result})" :
            nameof(Evaluation);

        [JsonConstructor]
        public Evaluation(JToken? result, EError? error = null)
        {
            Result = result;
            Error = error;
        }

        public Evaluation(object? result, EError? error = null) :
            this(result != null ? JToken.FromObject(result) : null, error)
        {
        }

        public Evaluation(EError error) : this(null, error ?? throw new ArgumentNullException(nameof(error)))
        {
        }

        public Evaluation(EErrorCode code, EError? innerError = null) :
            this(null, new EError(code, innerError))
        {
        }

        public string ToJson(Formatting formatting = Formatting.None)
        {
            return JsonConvert.SerializeObject(this, formatting, JsonHelper.IgnoreNullSettings);
        }

        public static implicit operator Evaluation(EError error)
        {
            return new Evaluation(error);
        }

        public static implicit operator Evaluation(double result)
        {
            return new Evaluation(result);
        }
    }
}
