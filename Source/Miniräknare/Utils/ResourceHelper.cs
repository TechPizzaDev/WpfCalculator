using System.Reflection;
using System.Resources;

namespace Miniräknare
{
    public static class ResourceHelper
    {
        public static ResourceReader GetResourceReader(Assembly resourceAssembly)
        {
            var resourceNames = resourceAssembly.GetManifestResourceNames();
            var resourceStream = resourceAssembly.GetManifestResourceStream(resourceNames[0]);
            return new ResourceReader(resourceStream);
        }
    }
}
