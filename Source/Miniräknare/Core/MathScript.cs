using System;
using System.Reflection;

namespace Miniräknare
{
    public class MathScript
    {
        private Assembly _assembly;
        private Type _scriptClass;

        public MathScript(Assembly assembly)
        {
            _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));

            _scriptClass = _assembly.GetType(MathScriptFactory.ScriptClassName);
            //var methodBase = .GetMethod(method.Name, new Type[0]);
            //
            //// 4. get il or even execute
            //var il = methodBase.GetMethodBody();
            //methodBase.Invoke(null, null);

            var methods = _scriptClass.GetMethods();
            Console.WriteLine(methods);

        }
    }
}
