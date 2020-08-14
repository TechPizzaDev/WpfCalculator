using System.Diagnostics.CodeAnalysis;
using System.IO;
using Newtonsoft.Json;

namespace WpfCalculator
{
    public static class JsonSerializerExtensions
    {
        [return: MaybeNull]
        public static T Deserialize<T>(this JsonSerializer serializer, Stream stream)
        {
            using var textReader = new StreamReader(stream, leaveOpen: true);
            var jsonReader = new JsonTextReader(textReader);
            return serializer.Deserialize<T>(jsonReader);
        }
    }
}
