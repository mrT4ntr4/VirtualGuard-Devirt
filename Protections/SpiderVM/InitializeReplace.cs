using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;
using System;
using System.Collections.Generic;
using VirtualGuardDevirt.Protections.SpiderVM.VMData;
using static VirtualGuardDevirt.Context;
using static VirtualGuardDevirt.Logger;

namespace VirtualGuardDevirt.Protections.SpiderVM
{
    internal class InitializeReplace
    {
        public static List<int> localsStore = new List<int>(new int[0x3c]);
        public static Stack<TypeSignature> typeInfoStack = new Stack<TypeSignature>();
        public static MethodDefinition _methodDef;

        public static void ReplacePhase()
        {
            MethodDefinition moduleCtr = module.GetModuleConstructor();
            Log($"NOPing out VM Init instructions from constructor ({moduleCtr.FullName})", TypeMessage.Info);
            var instrs = moduleCtr.CilMethodBody.Instructions;
            instrs.RemoveRange(instrs.Count - 4, 3);
            
            foreach (var methodvirt in VM.MethodVirt)
            {
                _methodDef = methodvirt.MethodDef;
                var instructions = _methodDef.CilMethodBody.Instructions;
                var methodLocalVars = _methodDef.CilMethodBody.LocalVariables;
                instructions.Clear();
                methodLocalVars.Clear();

                //assign new local variables to method
                for (int i = 0; i < 0xa; i++)
                    methodLocalVars.Add(new CilLocalVariable(module.CorLibTypeFactory.Object));

                Log($"Disassembling Method : {methodvirt.FullName}", TypeMessage.Info);
                var disasInstr = Disassemble(methodvirt, methodLocalVars);
                instructions.AddRange(disasInstr);
                instructions.CalculateOffsets();
                foreach (var instr in instructions)
                {
                    Console.WriteLine(instr);
                }
            }
        }

        public static List<CilInstruction> Disassemble(VMMethod _method, CilLocalVariableCollection methodLocalVars)
        {
            List<CilInstruction> newIns = new List<CilInstruction>();

            int FragAddr = _method.DisasConst;

            MethodDefinition _methodDef = _method.MethodDef;
            TypeSignature methodReturnType = _methodDef.Signature.ReturnType;

            int i = 0;
            var Instructions = VM.Methods[FragAddr];
            IMetadataMember resolvedMember;

            List<CilInstructionLabel> labels = new List<CilInstructionLabel>();
            for (int y = 0; y < Instructions.Count; y++)
            {
                labels.Add(new CilInstructionLabel());
            }

            while (i < Instructions.Count)
            {
                byte opcode = Instructions[i].Opcode;
                dynamic operand = Instructions[i].Operand;

                CilInstruction ins = null;
                switch (opcode)
                {
                    case 0x0:
                        ins = new CilInstruction(CilOpCodes.Add);
                        break;
                    case 0x1:
                        ins = new CilInstruction(CilOpCodes.Sub);
                        break;
                    case 0x2:
                        ins = new CilInstruction(CilOpCodes.Mul);
                        break;
                    case 0x3:
                        ins = new CilInstruction(CilOpCodes.Div);
                        break;
                    case 0x4:
                        ins = new CilInstruction(CilOpCodes.Stloc, methodLocalVars[operand]);
                        break;
                    case 0x5:
                        ins = new CilInstruction(CilOpCodes.Ldloc, methodLocalVars[operand]);
                        break;
                    case 0x6:
                        ins = new CilInstruction(CilOpCodes.Pop);
                        break;
                    case 0x7:
                        //Console.WriteLine($"ret");
                        var prevInstr = newIns[newIns.Count - 1];
                        if (prevInstr.OpCode == CilOpCodes.Ldloc)
                        {
                            var localVarIdx = prevInstr.GetLocalVariable(methodLocalVars).Index;
                            methodLocalVars[localVarIdx].VariableType = methodReturnType;
                        }
                        ins = new CilInstruction(CilOpCodes.Ret);
                        break;
                    case 0x8:
                        //Console.WriteLine($"push {operand}");
                        if (operand.GetType() == typeof(string))
                            ins = new CilInstruction(CilOpCodes.Ldstr, operand);
                        if (operand.GetType() == typeof(Int32))
                        {
                            ins = new CilInstruction(CilOpCodes.Ldc_I4, operand);

                        }
                        if (operand.GetType() == typeof(Int64))
                            ins = new CilInstruction(CilOpCodes.Ldc_I8, operand);
                        break;
                    case 0x9:
                        switch (operand)
                        {
                            case 0:
                                ins = new CilInstruction(CilOpCodes.Ldarg_0);
                                break;
                            case 1:
                                ins = new CilInstruction(CilOpCodes.Ldarg_1);
                                break;
                            case 2:
                                ins = new CilInstruction(CilOpCodes.Ldarg_2);
                                break;
                            case 3:
                                ins = new CilInstruction(CilOpCodes.Ldarg_3);
                                break;
                            default:
                                ins = new CilInstruction(CilOpCodes.Ldarg, operand);
                                break;
                        }
                        break;
                    case 0xa:
                        module.TryLookupMember((MetadataToken)operand, out resolvedMember);
                        //Console.WriteLine($"call 0x{operand:x8} => {resolvedMember}");
                        ins = new CilInstruction(CilOpCodes.Call, resolvedMember);
                        break;
                    case 0xb:
                        module.TryLookupMember((MetadataToken)operand, out resolvedMember);
                        //Console.WriteLine($"call 0x{operand:x8} => {resolvedMember}");
                        ins = new CilInstruction(CilOpCodes.Call, resolvedMember);
                        break;
                    case 0xc:
                        break;
                    case 0xd:
                        ins = new CilInstruction(CilOpCodes.Stfld);
                        break;
                    case 0xe:
                        ins = new CilInstruction(CilOpCodes.Ldfld);
                        break;
                    case 0xf:
                        ins = new CilInstruction(CilOpCodes.Newarr);
                        break;
                    case 0x10:
                        ins = new CilInstruction(CilOpCodes.Dup);
                        break;
                    case 0x11:
                        break;
                    case 0x12:
                        //Console.WriteLine($"br {labels[operand]}");
                        ins = new CilInstruction(CilOpCodes.Br, labels[operand + 1]);
                        break;
                    case 0x13:
                        ins = new CilInstruction(CilOpCodes.Ldelem);
                        break;
                    case 0x14:
                        ins = new CilInstruction(CilOpCodes.Ldelem_I);
                        break;
                    case 0x15:
                        ins = new CilInstruction(CilOpCodes.Ldelem_I1);
                        break;
                    case 0x16:
                        ins = new CilInstruction(CilOpCodes.Ldelem_I2);
                        break;
                    case 0x17:
                        ins = new CilInstruction(CilOpCodes.Ldelem_I4);
                        break;
                    case 0x18:
                        ins = new CilInstruction(CilOpCodes.Ldelem_I8);
                        break;
                    case 0x19:
                        ins = new CilInstruction(CilOpCodes.Ldelem_R4);
                        break;
                    case 0x1a:
                        ins = new CilInstruction(CilOpCodes.Ldelem_R8);
                        break;
                    case 0x1b:
                        ins = new CilInstruction(CilOpCodes.Ldelem_Ref);
                        break;
                    case 0x1c:
                        ins = new CilInstruction(CilOpCodes.Ldelem_U1);
                        break;
                    case 0x1d:
                        ins = new CilInstruction(CilOpCodes.Ldelem_U2);
                        break;
                    case 0x1e:
                        ins = new CilInstruction(CilOpCodes.Ldelem_U4);
                        break;
                    case 0x1f:
                        ins = new CilInstruction(CilOpCodes.Stelem);
                        break;
                    case 0x20:
                        ins = new CilInstruction(CilOpCodes.Stelem_I);
                        break;
                    case 0x21:
                        ins = new CilInstruction(CilOpCodes.Stelem_I1);
                        break;
                    case 0x22:
                        ins = new CilInstruction(CilOpCodes.Stelem_I2);
                        break;
                    case 0x23:
                        ins = new CilInstruction(CilOpCodes.Stelem_I4);
                        break;
                    case 0x24:
                        ins = new CilInstruction(CilOpCodes.Stelem_I8);
                        break;
                    case 0x25:
                        ins = new CilInstruction(CilOpCodes.Stelem_R4);
                        break;
                    case 0x26:
                        ins = new CilInstruction(CilOpCodes.Stelem_R8);
                        break;
                    case 0x27:
                        ins = new CilInstruction(CilOpCodes.Stelem_Ref);
                        break;
                    case 0x28:
                        ins = new CilInstruction(CilOpCodes.Ldarga);
                        break;
                    case 0x29:
                        ins = new CilInstruction(CilOpCodes.Ldloca);
                        break;
                    case 0x2a:
                        ins = new CilInstruction(CilOpCodes.Ldlen);
                        break;
                    case 0x2b:
                        //push(intPtr);
                        break;
                    case 0x2c:
                        ins = new CilInstruction(CilOpCodes.Conv_I1);
                        break;
                    case 0x2d:
                        ins = new CilInstruction(CilOpCodes.Conv_I2);
                        break;
                    case 0x2e:
                        ins = new CilInstruction(CilOpCodes.Conv_I4);
                        break;
                    case 0x2f:
                        ins = new CilInstruction(CilOpCodes.Conv_I8);
                        break;
                    case 0x30:
                        ins = new CilInstruction(CilOpCodes.Conv_R_Un);
                        break;
                    case 0x31:
                        ins = new CilInstruction(CilOpCodes.Conv_R4);
                        break;
                    case 0x32:
                        ins = new CilInstruction(CilOpCodes.Conv_R8);
                        break;
                    case 0x33:
                        //push(uintPtr);
                        break;
                    case 0x34:
                        ins = new CilInstruction(CilOpCodes.Conv_U1);
                        break;
                    case 0x35:
                        ins = new CilInstruction(CilOpCodes.Conv_U2);
                        break;
                    case 0x36:
                        ins = new CilInstruction(CilOpCodes.Conv_U4);
                        break;
                    case 0x37:
                        ins = new CilInstruction(CilOpCodes.Conv_U8);
                        break;
                    case 0x38:
                        //Console.WriteLine($"beq {labels[operand]}");
                        ins = new CilInstruction(CilOpCodes.Beq, labels[operand + 1]);
                        break;
                    case 0x39:
                        ins = new CilInstruction(CilOpCodes.Bge, labels[operand + 1]);
                        break;
                    case 0x3a:
                        ins = new CilInstruction(CilOpCodes.Bgt, labels[operand + 1]);
                        break;
                    case 0x3b:
                        //Console.WriteLine($"ble {labels[operand]}");
                        ins = new CilInstruction(CilOpCodes.Ble, labels[operand + 1]);
                        break;
                    case 0x3c:
                        ins = new CilInstruction(CilOpCodes.Blt, labels[operand + 1]);
                        break;
                    case 0x3d:
                        ins = new CilInstruction(CilOpCodes.Brfalse);
                        break;
                    case 0x3e:
                        ins = new CilInstruction(CilOpCodes.Brtrue);
                        break;
                    case 0x3f:
                        ins = new CilInstruction(CilOpCodes.Xor);
                        break;
                    case 0x40:
                        //Console.WriteLine($"br {labels[operand]}");
                        ins = new CilInstruction(CilOpCodes.Br, labels[operand + 1]);
                        break;
                    case 0x41:
                        ins = new CilInstruction(CilOpCodes.Ceq);
                        break;
                    case 0xff:
                        ins = new CilInstruction(CilOpCodes.Nop);
                        break;
                    default:
                        throw new Exception($"Unknown VM Opcode (0x{opcode:x2})");
                }
                labels[i].Instruction = ins;
                newIns.Add(ins);
                i++;
            }
            return newIns;


        }

    }
}