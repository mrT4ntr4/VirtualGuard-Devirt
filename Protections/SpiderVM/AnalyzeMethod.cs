using AsmResolver.DotNet;
using AsmResolver.DotNet.Collections;
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.PE.DotNet.Cil;
using System.Collections.Generic;
using System.Linq;
using VirtualGuardDevirt.Protections.SpiderVM.VMData;
using static VirtualGuardDevirt.Context;
using static VirtualGuardDevirt.Logger;

namespace VirtualGuardDevirt.Protections.SpiderVM
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
                    if (instrCount >= 6)
                    {
                        CilInstruction theInstr = methodInstr[instrCount - 3];
                        if (theInstr.OpCode == CilOpCodes.Call)
                        {
                            IMethodDescriptor methodDesc = (IMethodDescriptor)theInstr.Operand;

                            MethodDefinition methodDef = methodDesc.Resolve();

                            if (methodDef.Parameters.Count() == 2 && methodDef.Parameters[0].ToString().Contains("System.Object[]") && methodDef.Parameters[1].ToString().Contains("System.Int32"))
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

                                if (VM.VMType == null)
                                {

                                    VM.VMType = methodDef.DeclaringType;

                                    Log($"Found VM Method Type: {VM.VMType.FullName}", TypeMessage.Info);
                                }

                                Log($"Found Virtualized method : {method.FullName} with disasConst : 0x{disasConst:x8}", TypeMessage.Info);
                                VM.MethodVirt.Add(new VMMethod(method.FullName, disasConst, paramTypes, method));
                            }
                        }
                    }
                }
            }
        }
    }
}