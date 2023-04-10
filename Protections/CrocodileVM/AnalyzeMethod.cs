using AsmResolver.DotNet;
using AsmResolver.DotNet.Collections;
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.PE.DotNet.Cil;
using System.Collections.Generic;
using System.Linq;
using VirtualGuardDevirt.Protections.CrocodileVM.VMData;
using static VirtualGuardDevirt.Context;
using static VirtualGuardDevirt.Logger;

namespace VirtualGuardDevirt.Protections.CrocodileVM
{
    internal class AnalyzeMethod
    {
        internal static void AnalysePhase()
        {
            foreach (var type in module.TopLevelTypes)
            {
                foreach (var method in type.Methods.Where(n => n.CilMethodBody != null))
                {
                    var methodInstr = method.CilMethodBody.Instructions;
                    var instrCount = methodInstr.Count;
                    if (instrCount >= 12)
                    {
                        //TODO : Better Signature
                        if (methodInstr[instrCount - 3].OpCode == CilOpCodes.Call
                            && methodInstr[instrCount - 3].Operand.ToString().Contains("A4FD01::2CDA08"))
                        {
                            int disasConst = methodInstr[instrCount - 4].GetLdcI4Constant();
                            List<TypeSignature> paramTypes = new List<TypeSignature>();

                            foreach (var item in methodInstr)
                            {
                                if (item.OpCode == CilOpCodes.Ldarg)
                                {
                                    Parameter pp = item.Operand as Parameter;
                                    paramTypes.Add(pp.ParameterType);
                                }
                            }
                            Log($"Found Virtualized method : {method.FullName} with disasConst : 0x{disasConst:x8}", TypeMessage.Info);
                            VM.MethodVirt.Add(new VMMethod(method.FullName, disasConst, paramTypes, method));
                        }
                    }
                }
                if(type.FullName == "A4FD01") VM.VMType = type;
            }
        }
    }
}