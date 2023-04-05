using AsmResolver.DotNet;
using System.Linq;
using static VirtualGuardDevirt.Context;

namespace VirtualGuardDevirt.Protections.CrocodileVM
{
    internal class AnalyzeResources
    {
        internal static void InitialiseResources()
        {
            ManifestResource resource = (from x in module.Resources where x.Name == "crocodile" select x).First();
            VM.CrocodileBytecode = resource.GetData();
        }
    }
}