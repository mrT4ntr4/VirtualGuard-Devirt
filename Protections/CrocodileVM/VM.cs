using System.Collections.Generic;
using VirtualGuardDevirt.Protections.CrocodileVM.VMData;

namespace VirtualGuardDevirt.Protections.CrocodileVM
{
    internal static class VM
    {
        public static byte[] CrocodileBytecode;
        public static Dictionary<int, List<VMInstruction>> Blocks = new Dictionary<int, List<VMInstruction>>();
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
