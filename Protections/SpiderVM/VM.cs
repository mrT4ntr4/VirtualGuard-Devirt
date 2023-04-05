using System.Collections.Generic;
using VirtualGuardDevirt.Protections.SpiderVM.VMData;

namespace VirtualGuardDevirt.Protections.SpiderVM
{
    internal static class VM
    {
        public static byte[] SpiderBytecode;
        public static Dictionary<int, List<VMInstruction>> Methods = new Dictionary<int, List<VMInstruction>>();
        public static List<VMMethod> MethodVirt = new List<VMMethod>();

        internal static void Execute()
        {
            AnalyzeResources.InitialiseResources();
            AnalyzeMethod.AnalysePhase();
            InitializeMethod.InitiliaseMethodage();
            InitializeReplace.ReplacePhase();
        }
    }
}
