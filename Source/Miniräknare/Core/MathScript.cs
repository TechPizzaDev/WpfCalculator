using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Miniräknare
{
    public class MathScript
    {
        private static Dictionary<string, Type> _typeMap;

        static MathScript()
        {
            _typeMap = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

            //AddType(typeof(Op));
            AddType(typeof(MathLib.Strengths.Force));
            AddType(typeof(MathLib.Space.Area));
        }

        public MathScript(MathScriptData data) : this(data.Input, data.Output)
        {
        }

        public MathScript(object input, object output)
        {
            if (!(input is XmlNode[] inputNodes))
                throw new ArgumentException(nameof(input));

            if (!(output is XmlNode[] outputNodes))
                throw new ArgumentException(nameof(output));

            var inputTypes = NodesToTypes(inputNodes);
            var outputTypes = NodesToTypes(outputNodes);

            Console.WriteLine("HEH");
        }

        #region Type Management

        private static void AddType(Type type)
        {
            _typeMap.Add(type.Name, type);
        }

        private static IEnumerable<Type> NodesToTypes(XmlNode[] nodes)
        {
            return nodes.Select(x =>
            {
                if (!_typeMap.TryGetValue(x.Name, out Type type))
                    throw new Exception($"Unknown type \"{x.Name}\".");
                return type;
            });
        }

        #endregion
    }
}
