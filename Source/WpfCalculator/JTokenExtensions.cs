using Newtonsoft.Json.Linq;

namespace WpfCalculator
{
    public static class JTokenExtensions
    {
        public static bool IsNumber(this JToken token)
        {
            return token.Type == JTokenType.Integer || token.Type == JTokenType.Float;
        }
    }
}
