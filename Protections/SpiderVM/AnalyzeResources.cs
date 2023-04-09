using AsmResolver.DotNet;
using System.Linq;
using static VirtualGuardDevirt.Context;

namespace VirtualGuardDevirt.Protections.SpiderVM
{
    internal class AnalyzeResources
    {
        internal static void InitialiseResources()
        {
            ManifestResource resource = (from x in module.Resources where x.Name == "spider" select x).First();
            VM.SpiderBytecode = resource.GetData();
            VM.VMResource = resource;
        }
    }
}