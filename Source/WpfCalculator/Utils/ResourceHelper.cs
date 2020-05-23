using System;
using System.Reflection;
using System.Resources;

namespace WpfCalculator
{
    public static class ResourceHelper
    {
        public static ResourceReader GetResourceReader(Assembly resourceAssembly)
        {
            var resourceNames = resourceAssembly.GetManifestResourceNames();
            var resourceStream = resourceAssembly.GetManifestResourceStream(resourceNames[0]);
            return new ResourceReader(resourceStream);
        }

        public static Uri MakePackUri(string relativeFile)
        {
            return MakePackUri(Assembly.GetCallingAssembly(), relativeFile);
        }

        public static Uri MakePackUri(Assembly assembly, string relativeFile)
        {
            // Extract the short name.
            string assemblyShortName = assembly.ToString().Split(',')[0];

            string uriString = "pack://application:,,,/" +
                assemblyShortName +
                ";component/" +
                relativeFile;

            return new Uri(uriString);
        }
    }
}
