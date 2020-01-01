using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Runtime.Loader;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CSharp.RuntimeBinder;

namespace Miniräknare
{
    public static class MathScriptFactory
    {
        private static bool _initialized;

        private static CSharpParseOptions _parseOptions;
        private static MetadataReference[] _metadataReferences;
        private static CSharpCompilationOptions _compilationOptions;

        public const string ScriptClassName = "MathScriptAction";

        public static void Initialize()
        {
            if (_initialized)
                return; 

            _parseOptions = new CSharpParseOptions();

            var netStandardName = new AssemblyName("netstandard");
            var netStandardAssembly = AssemblyLoadContext.Default.LoadFromAssemblyName(netStandardName);
            _metadataReferences = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(AssemblyTargetedPatchBandAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(CSharpArgumentInfo).Assembly.Location),
                MetadataReference.CreateFromFile(netStandardAssembly.Location),
                MetadataReference.CreateFromFile(typeof(RuntimeBinderException).Assembly.Location), // required for 'dynamic'

                MetadataReference.CreateFromFile(typeof(BoxedValue).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(MathLib.InternationalUnit).Assembly.Location)
            };

            _compilationOptions = new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Release,
                assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default);

            _initialized = true;
        }

        public static MathScript Compile(
            StringBuilder temporaryBuilder, string actionCode, bool generateSymbols)
        {
            Initialize();

            /*
            var script = CSharpScript.Create(
                code, options, globalsType: typeof(MathFunctionState));

            var args = new[] 
            {
                new InputArgument(null, new BoxedValue<Force>(Force.FromNewtons(200))),
                new InputArgument(null, new BoxedValue<Area>(Area.FromMilliMeters(100)))
            };
            var state = new MathFunctionState(args);

            var runner = script.CreateDelegate();
            var task = runner.Invoke(state);
            task.Wait(1000);

            Console.WriteLine(task.Result);
            */

            temporaryBuilder.Clear();

            ExpandActionCode(actionCode, temporaryBuilder);
            string sourceCode = temporaryBuilder.ToString();
            var sourceText = SourceText.From(sourceCode);

            //var w = new StringWriter();
            //sourceText.Write(w);
            //Console.WriteLine(w.ToString());

            var syntaxTree = CSharpSyntaxTree.ParseText(sourceText, _parseOptions);

            var compilation = CSharpCompilation.Create(
                assemblyName: "Drag",
                new[] { syntaxTree },
                _metadataReferences,
                _compilationOptions);

            using var dll = new MemoryStream();
            using var pdb = generateSymbols ? new MemoryStream() : null;
            var emitResult = compilation.Emit(dll, pdb);
            dll.Seek(0, SeekOrigin.Begin);
            pdb?.Seek(0, SeekOrigin.Begin);

            if (!emitResult.Success)
            {
                temporaryBuilder.Clear();
                temporaryBuilder.AppendLine("Function compilation failed:");

                var fatalErrors = emitResult.Diagnostics.Where(
                    diagnostic => diagnostic.IsWarningAsError ||
                    diagnostic.Severity == DiagnosticSeverity.Error);

                foreach (var diagnostic in fatalErrors)
                {
                    temporaryBuilder.Append(diagnostic.Severity.ToString());
                    temporaryBuilder.Append(": ");
                    temporaryBuilder.AppendLine(diagnostic.GetMessage());
                }
                throw new Exception(temporaryBuilder.ToString());
            }

            var assemblyContext = new UnloadableAssemblyLoadContext();
            var assembly = assemblyContext.LoadFromStream(dll, pdb);

            return new MathScript(assembly);
        }

        public static void ExpandActionCode(string actionCode, StringBuilder output)
        {
            var imports = new[]
            {
                "System",
                nameof(Miniräknare),
                "MathLib.Space",
                "MathLib.Strengths",
                "MathLib.Systems"
            };

            var formatArgs = new[]
            {
                ScriptClassName,
                nameof(MathScriptActionBase),
                actionCode
            };

            foreach (var import in imports)
            {
                output.Append("using ");
                output.Append(import);
                output.AppendLine(";");
            }

            output.AppendFormat(@"
public class {0} : {1}
{{
    public {0}(InputArgument[] arguments) : base(arguments)
    {{
    }}

    public BoxedValue Execute()
    {{
        {2}
    }}
}}", formatArgs);
        }
    }
}
