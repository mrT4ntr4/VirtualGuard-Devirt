using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures.Types;
using System.Collections.Generic;

namespace VirtualGuardDevirt.Protections.SpiderVM.VMData
{
    public class VMMethod
    {
        public string FullName { get; set; }
        public int DisasConst { get; set; }
        public List<TypeSignature> ParameterTypes { get; set; }
        public MethodDefinition MethodDef { get; set; }
        public VMMethod(string FullName, int disasConst, List<TypeSignature> parameterTypes, MethodDefinition methodDef)
        {
            this.FullName = FullName;
            this.DisasConst = disasConst;
            this.ParameterTypes = parameterTypes;
            this.MethodDef = methodDef;
        }

    }
}
