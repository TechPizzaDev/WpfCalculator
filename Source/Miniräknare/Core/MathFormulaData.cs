using System.IO;
using Newtonsoft.Json;

namespace Miniräknare
{
    [JsonObject]
    public class MathFormulaData
    {
        public string[] Inputs { get; set; }
        public string[] Outputs { get; set; }

        public MathFormulaData()
        {
        }

        public static MathFormulaData Load(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            return App.Serializer.Deserialize<MathFormulaData>(stream);
        }
    }
}
