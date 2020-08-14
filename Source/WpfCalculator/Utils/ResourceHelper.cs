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
            foreach (string name in resourceNames)
            {
                if (name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
                {
                    var resourceStream = resourceAssembly.GetManifestResourceStream(name);
                    if (resourceStream == null)
                        throw new ArgumentException(
                            $"Failed to get manifest resource stream for \"{name}\".",
                            nameof(resourceAssembly));

                    return new ResourceReader(resourceStream);
                }
            }
            throw new ArgumentException("Failed to find resources in assembly.", nameof(resourceAssembly));
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
