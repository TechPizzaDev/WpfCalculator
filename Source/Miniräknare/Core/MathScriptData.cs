using System.IO;
using System.Xml.Serialization;

namespace Miniräknare
{
    [XmlRoot(nameof(MathScript))]
    public class MathScriptData
    {
        public static XmlSerializer Serializer { get; } = new XmlSerializer(typeof(MathScriptData));

        public object Input { get; set; }
        public object Output { get; set; }

        public MathScriptData()
        {
        }

        public static MathScriptData Load(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            return Serializer.Deserialize(stream) as MathScriptData;
        }
    }
}
