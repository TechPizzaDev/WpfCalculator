using System.IO;
using Newtonsoft.Json;

namespace Miniräknare
{
    [JsonObject]
    public class EquationSetData
    {
        public string Equation { get; set; }

        public EquationSetData()
        {
        }

        public static EquationSetData Load(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            return App.Serializer.Deserialize<EquationSetData>(stream);
        }
    }
}
