using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Miniräknare
{
    public class ReturnValueData : IXmlSerializable
    {
        public string[] Types { get; private set; }
        public string[] Units { get; private set; }

        public ReturnValueData()
        {
            Types = Array.Empty<string>();
            Units = Array.Empty<string>();
        }

        #region IXmlSerializable

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            bool wasEmpty = reader.IsEmptyElement;
            reader.Read();
            if (wasEmpty)
                return;

            reader.MoveToContent();
            reader.ReadStartElement(nameof(Types));

            while (reader.Name != nameof(Types))
            {
                reader.Skip();
            }
            reader.ReadEndElement();

            reader.MoveToContent();
            reader.ReadStartElement(nameof(Units));
            while (reader.Name != nameof(Units))
            {
                reader.Skip();
            }
            reader.ReadEndElement();

            reader.ReadEndElement();
        }

        public void WriteXml(XmlWriter writer)
        {

        }

        #endregion
    }

    [XmlRoot(nameof(MathScript))]
    public class MathScriptData : IXmlSerializable
    {
        public static XmlSerializer Serializer { get; } = new XmlSerializer(typeof(MathScriptData));

        public string[] Input { get; private set; }
        public string Action { get; private set; }

        public ReturnValueData ReturnValue { get; }

        public bool IsConstant => Input.Length == 0;

        public MathScriptData()
        {
            Input = Array.Empty<string>();
            Action = string.Empty;
            ReturnValue = new ReturnValueData();
        }

        public static MathScriptData Load(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            return Serializer.Deserialize(stream) as MathScriptData;
        }

        public MathScript Compile(StringBuilder temporaryBuilder, bool generateSymbols)
        {
            try
            {
                return MathScriptFactory.Compile(temporaryBuilder, Action, generateSymbols);
            }
            catch(Exception ex)
            {
                throw new Exception("Failed to compile math function.", ex);
            }
        }

        #region IXmlSerializable

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            bool wasEmpty = reader.IsEmptyElement;
            reader.Read();
            if (wasEmpty)
                return;

            reader.ReadStartElement(nameof(Input));
            var inputList = new List<string>();
            while (reader.Name != nameof(Input))
            {
                string elementName = reader.Name;
                inputList.Add(elementName);

                reader.ReadStartElement();
            }
            Input = inputList.ToArray();
            reader.ReadEndElement();

            ReturnValue.ReadXml(reader);

            reader.ReadStartElement(nameof(Action));
            Action = reader.ReadContentAsString();
            reader.ReadEndElement();

            reader.ReadEndElement();
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(nameof(Input));
            if (Input != null)
            {
                foreach (var input in Input)
                {
                    //writer.WriteStartElement(input.GetType().Name);
                    writer.WriteStartElement(input.ToString());
                    writer.WriteEndElement();
                }
            }
            writer.WriteEndElement();

            ReturnValue.WriteXml(writer);

            writer.WriteStartElement(nameof(Action));
            writer.WriteCData(Action);
            writer.WriteEndElement();
        }

        #endregion
    }
}
