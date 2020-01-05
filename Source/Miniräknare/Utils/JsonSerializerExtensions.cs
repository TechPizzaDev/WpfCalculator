using System.IO;
using Newtonsoft.Json;

namespace Miniräknare
{
    public static class JsonSerializerExtensions
    {
        public static T Deserialize<T>(this JsonSerializer serializer, Stream stream)
        {
            using var textReader = new StreamReader(stream);
            var jsonReader = new JsonTextReader(textReader);
            return serializer.Deserialize<T>(jsonReader);
        }
    }
}
