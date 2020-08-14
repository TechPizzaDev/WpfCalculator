using Newtonsoft.Json;

namespace WpfCalculator
{
    public static class JsonHelper
    {
        public static JsonSerializerSettings IgnoreNullSettings { get; } = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };
    }
}