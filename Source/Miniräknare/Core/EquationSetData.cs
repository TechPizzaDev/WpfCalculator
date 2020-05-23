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
            return App.Serializer.Deserialize<EquationSetData>(stream);
        }
    }
}
