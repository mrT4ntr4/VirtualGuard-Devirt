using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Serialized;
using AsmResolver.DotNet.Signatures;
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using System;
using System.Collections.Generic;
using System.Linq;
using VirtualGuardDevirt.Protections.CrocodileVM.VMData;
using static VirtualGuardDevirt.Context;
using static VirtualGuardDevirt.Logger;

namespace VirtualGuardDevirt.Protections.CrocodileVM
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
            instrs.RemoveRange(0, 2);

            foreach (var methodvirt in VM.MethodVirt)
            {
                _methodDef = methodvirt.MethodDef;
                var instructions = _methodDef.CilMethodBody.Instructions;
                var methodLocalVars = _methodDef.CilMethodBody.LocalVariables;
                instructions.Clear();
                methodLocalVars.Clear();

                //assign new local variables to method
                for (int i = 0; i < 0x3c; i++)
                    methodLocalVars.Add(new CilLocalVariable(module.CorLibTypeFactory.Object));
                methodLocalVars[0x3b].VariableType = module.CorLibTypeFactory.Boolean;

                Log($"Disassembling Method : {methodvirt.FullName}", TypeMessage.Info);
                List<CilInstruction> disasInstr = Disassemble(methodvirt, methodLocalVars);

                instructions.AddRange(disasInstr);
                instructions.CalculateOffsets();
                foreach (var instr in instructions)
                {
                    Console.WriteLine(instr);
                }
            }
            module.TopLevelTypes.Remove(VM.VMType);
            module.Resources.Remove(VM.VMResource);
        }

        public static List<CilInstruction> Disassemble(VMMethod _method, CilLocalVariableCollection methodLocalVars)
        {
            Stack<(int, CilInstructionLabel)> simpleBrStack = new Stack<(int, CilInstructionLabel)>();
            Stack<(int, CilInstructionLabel)> condBrStack = new Stack<(int, CilInstructionLabel)>();
            List<CilInstruction> newIns = new List<CilInstruction>();
            Dictionary<int, List<CilInstruction>> MethodBlocks = new Dictionary<int, List<CilInstruction>>();

            int BlockAddr = _method.DisasConst;

            MethodDefinition _methodDef = _method.MethodDef;


            simpleBrStack.Push((BlockAddr, new CilInstructionLabel()));

            while (simpleBrStack.Count > 0 || condBrStack.Count > 0)
            {
                int _currentBlockAddr = 0;
                CilInstructionLabel _currentBlockLabel = null;

                if (simpleBrStack.Count > 0)
                {
                    var simpleBrData = simpleBrStack.Pop();
                    Log("Disassembling Simple Branch! ", TypeMessage.Debug);
                    _currentBlockAddr = simpleBrData.Item1;
                    _currentBlockLabel = simpleBrData.Item2;
                }
                else if (condBrStack.Count > 0)
                {
                    var condBrData = condBrStack.Pop();
                    Log("Disassembling Conditional Branch! ", TypeMessage.Debug);
                    _currentBlockAddr = condBrData.Item1;
                    _currentBlockLabel = condBrData.Item2;
                }

                Log($"_currentBlockAddr = 0x{_currentBlockAddr:x8}", TypeMessage.Debug);
                if (MethodBlocks.ContainsKey(_currentBlockAddr))
                {
                    Log("Already Disassembled!", TypeMessage.Debug);
                    _currentBlockLabel.Instruction = MethodBlocks[_currentBlockAddr][0];
                    continue;
                }

                var Instructions = VM.Blocks[_currentBlockAddr];
                List<CilInstruction> BlockIns = new List<CilInstruction>();
                int i = 0;
                IMetadataMember resolvedMember = null;

                while (i < Instructions.Count)
                {
                    var ins = Instructions[i];

                    byte opcode = ins.Opcode;
                    int operand1 = ins.Operand1;
                    var operand2 = ins.Operand2;

                    int destAddr;
                    CilInstructionLabel condBrLabel;
                    CilInstructionLabel simpleBrLabel;

                    switch (opcode)
                    {
                        case 0x0:
                            if (operand1 != 0x3c)
                            {
                                //Console.WriteLine($"stloc V_{operand1}");
                                CilInstruction prevInstr = BlockIns[BlockIns.Count - 1];
                                if (prevInstr.OpCode == CilOpCodes.Call
                                    || prevInstr.OpCode == CilOpCodes.Newobj
                                    || prevInstr.OpCode == CilOpCodes.Ldfld)
                                {
                                    var poppedType = typeInfoStack.Pop();
                                    methodLocalVars[operand1].VariableType = poppedType;
                                }
                                BlockIns.Add(new CilInstruction(CilOpCodes.Stloc, methodLocalVars[operand1]));
                            }
                            break;
                        case 0x1:
                            //Console.WriteLine($"ceq V_{operand1} V_{operand2}");
                            BlockIns.Add(new CilInstruction(CilOpCodes.Ldloc, methodLocalVars[operand1]));
                            BlockIns.Add(new CilInstruction(CilOpCodes.Ldloc, methodLocalVars[operand2]));
                            BlockIns.Add(new CilInstruction(CilOpCodes.Ceq));
                            BlockIns.Add(new CilInstruction(CilOpCodes.Stloc, methodLocalVars[0x3b]));
                            break;
                        case 0x2:
                            if (operand2.GetType() == typeof(string))
                            {
                                //Console.WriteLine($"ldstr \"{operand2}\"; stloc V_{operand1}");
                                BlockIns.Add(new CilInstruction(CilOpCodes.Ldstr, operand2));
                                methodLocalVars[operand1].VariableType = module.CorLibTypeFactory.String;
                                BlockIns.Add(new CilInstruction(CilOpCodes.Stloc, methodLocalVars[operand1]));
                            }
                            else if (operand2.GetType() == typeof(Int32))
                            {
                                localsStore[operand1] = (int)operand2;
                                BlockIns.Add(new CilInstruction(CilOpCodes.Ldc_I4, operand2));
                                methodLocalVars[operand1].VariableType = module.CorLibTypeFactory.Int32;
                                BlockIns.Add(new CilInstruction(CilOpCodes.Stloc, methodLocalVars[operand1]));
                            }
                            else if (operand2.GetType() == typeof(Int64))
                            {
                                BlockIns.Add(new CilInstruction(CilOpCodes.Ldc_I8, operand2));
                                methodLocalVars[operand1].VariableType = module.CorLibTypeFactory.Int64;
                                BlockIns.Add(new CilInstruction(CilOpCodes.Stloc, methodLocalVars[operand1]));
                            }
                            break;
                        case 0x3:
                            BlockIns.Add(new CilInstruction(CilOpCodes.Ldloc, methodLocalVars[operand2]));
                            BlockIns.Add(new CilInstruction(CilOpCodes.Stloc, methodLocalVars[operand1]));
                            break;
                        case 0x4:
                            //Console.WriteLine($"brtrue V_{operand1}");
                            destAddr = localsStore[operand1];
                            condBrLabel = new CilInstructionLabel();
                            BlockIns.Add(new CilInstruction(CilOpCodes.Ldloc, methodLocalVars[0x3b]));
                            BlockIns.Add(new CilInstruction(CilOpCodes.Brtrue, condBrLabel));
                            condBrStack.Push((destAddr, condBrLabel));
                            break;
                        case 0x6:
                            if (operand1 == 3)
                            {
                                module.TryLookupMember((MetadataToken)operand2, out IMetadataMember field);
                                //Console.WriteLine($"stlfd {field}");
                                BlockIns.Reverse(BlockIns.Count - 2, 2);
                                BlockIns.Add(new CilInstruction(CilOpCodes.Stfld, field));
                            }
                            else if (operand1 == 2)
                            {
                                module.TryLookupMember((MetadataToken)operand2, out IMetadataMember field);
                                //Console.WriteLine($"ldfld {field}");
                                FieldDefinition fieldDef = field as FieldDefinition;
                                BlockIns.Add(new CilInstruction(CilOpCodes.Ldfld, field));
                                typeInfoStack.Push(fieldDef.Signature.FieldType);
                            }
                            break;
                        case 0x7:
                            //Console.WriteLine($"sub V_{operand1} V_{operand2}");
                            BlockIns.Add(new CilInstruction(CilOpCodes.Ldloc, methodLocalVars[operand1]));
                            BlockIns.Add(new CilInstruction(CilOpCodes.Ldloc, methodLocalVars[operand2]));
                            BlockIns.Add(new CilInstruction(CilOpCodes.Sub));
                            BlockIns.Add(new CilInstruction(CilOpCodes.Stloc, methodLocalVars[operand1]));
                            break;
                        case 0x8:
                            //Console.WriteLine($"add V_{operand1} V_{operand2}");
                            BlockIns.Add(new CilInstruction(CilOpCodes.Ldloc, methodLocalVars[operand1]));
                            BlockIns.Add(new CilInstruction(CilOpCodes.Ldloc, methodLocalVars[operand2]));
                            BlockIns.Add(new CilInstruction(CilOpCodes.Add));
                            BlockIns.Add(new CilInstruction(CilOpCodes.Stloc, methodLocalVars[operand1]));
                            break;
                        case 0x9:
                            //Console.WriteLine($"cgt V_{operand1} V_{operand2}");
                            BlockIns.Add(new CilInstruction(CilOpCodes.Ldloc, methodLocalVars[operand1]));
                            BlockIns.Add(new CilInstruction(CilOpCodes.Ldloc, methodLocalVars[operand2]));
                            BlockIns.Add(new CilInstruction(CilOpCodes.Ceq));
                            BlockIns.Add(new CilInstruction(CilOpCodes.Stloc, methodLocalVars[0x3b]));
                            break;
                        case 0xc:
                            //Console.WriteLine($"add V_{operand1} V_{operand2}");
                            BlockIns.Add(new CilInstruction(CilOpCodes.Ldloc, methodLocalVars[operand1]));
                            BlockIns.Add(new CilInstruction(CilOpCodes.Ldloc, methodLocalVars[operand2]));
                            BlockIns.Add(new CilInstruction(CilOpCodes.Div));
                            BlockIns.Add(new CilInstruction(CilOpCodes.Stloc, methodLocalVars[operand1]));
                            break;
                        case 0xd:
                            //Console.WriteLine($"cgt V_{operand1} V_{operand2}");
                            BlockIns.Add(new CilInstruction(CilOpCodes.Ldloc, methodLocalVars[operand1]));
                            BlockIns.Add(new CilInstruction(CilOpCodes.Ldloc, methodLocalVars[operand2]));
                            BlockIns.Add(new CilInstruction(CilOpCodes.Cgt));
                            BlockIns.Add(new CilInstruction(CilOpCodes.Stloc, methodLocalVars[0x3b]));
                            break;
                        case 0xf:
                            module.TryLookupMember((MetadataToken)operand2, out resolvedMember);
                            //Console.WriteLine($"resolve method 0x{operand2:x8} => V_{operand1} ({resolvedMember})");
                            break;
                        case 0x10:
                            //Console.WriteLine($"ret V_{operand1}");
                            if (_methodDef.Signature.ReturnsValue)
                                BlockIns.Add(new CilInstruction(CilOpCodes.Ldloc, methodLocalVars[operand1]));
                            BlockIns.Add(new CilInstruction(CilOpCodes.Ret));
                            break;
                        case 0x12:
                            //Console.WriteLine($"ldloc V_{operand1}");
                            BlockIns.Add(new CilInstruction(CilOpCodes.Ldloc, methodLocalVars[operand1]));
                            break;
                        case 0x13:
                            //Console.WriteLine($"clt V_{operand1} V_{operand2}");
                            BlockIns.Add(new CilInstruction(CilOpCodes.Ldloc, methodLocalVars[operand1]));
                            BlockIns.Add(new CilInstruction(CilOpCodes.Ldloc, methodLocalVars[operand2]));
                            BlockIns.Add(new CilInstruction(CilOpCodes.Clt));
                            BlockIns.Add(new CilInstruction(CilOpCodes.Stloc, methodLocalVars[0x3b]));
                            break;
                        case 0x14:
                            //Console.WriteLine($"brfalse V_{operand1}");
                            destAddr = localsStore[operand1];
                            condBrLabel = new CilInstructionLabel();
                            BlockIns.Add(new CilInstruction(CilOpCodes.Ldloc, methodLocalVars[0x3b]));
                            BlockIns.Add(new CilInstruction(CilOpCodes.Brfalse, condBrLabel));
                            condBrStack.Push((destAddr, condBrLabel));
                            break;
                        case 0x15:
                            //Console.WriteLine($"ldarg.{operand1}");
                            switch (operand1)
                            {
                                case 0:
                                    BlockIns.Add(new CilInstruction(CilOpCodes.Ldarg_0));
                                    break;
                                case 1:
                                    BlockIns.Add(new CilInstruction(CilOpCodes.Ldarg_1));
                                    break;
                                case 2:
                                    BlockIns.Add(new CilInstruction(CilOpCodes.Ldarg_2));
                                    break;
                                case 3:
                                    BlockIns.Add(new CilInstruction(CilOpCodes.Ldarg_3));
                                    break;
                                default:
                                    BlockIns.Add(new CilInstruction(CilOpCodes.Ldarg, operand1));
                                    break;
                            }
                            typeInfoStack.Push(_method.ParameterTypes[operand1]);
                            break;
                        case 0x16:
                            //Console.WriteLine($"call V_{operand1}");
                            TypeSignature returnType = null;
                            if (resolvedMember is SerializedMemberReference)
                            {
                                MemberReference memRef = (MemberReference)resolvedMember;
                                MethodSignature methSig = memRef.Signature as MethodSignature;
                                returnType = methSig.ReturnType;
                                BlockIns.Add(new CilInstruction(CilOpCodes.Call, resolvedMember));
                            }
                            if (resolvedMember is SerializedMethodDefinition)
                            {
                                MethodDefinition resolvedMethod = (MethodDefinition)resolvedMember;
                                returnType = resolvedMethod.Signature.ReturnType;

                                int numParams = resolvedMethod.Parameters.Count;
                                BlockIns.Reverse(BlockIns.Count - numParams, numParams);
                                if (resolvedMethod.IsConstructor)
                                {
                                    BlockIns.Add(new CilInstruction(CilOpCodes.Newobj, resolvedMember));
                                    returnType = resolvedMethod.DeclaringType.ToTypeSignature();
                                }
                                else
                                {
                                    BlockIns.Add(new CilInstruction(CilOpCodes.Call, resolvedMember));
                                }
                            }
                            if (returnType.ElementType != ElementType.Void)
                                typeInfoStack.Push(returnType);
                            break;
                        case 0x17:
                            //Console.WriteLine($"mul V_{operand1} V_{operand2}");
                            BlockIns.Add(new CilInstruction(CilOpCodes.Ldloc, methodLocalVars[operand1]));
                            BlockIns.Add(new CilInstruction(CilOpCodes.Ldloc, methodLocalVars[operand2]));
                            BlockIns.Add(new CilInstruction(CilOpCodes.Mul));
                            BlockIns.Add(new CilInstruction(CilOpCodes.Stloc, methodLocalVars[operand1]));
                            break;
                        case 0x18:
                            //Console.WriteLine($"br V_{operand1}");
                            simpleBrLabel = new CilInstructionLabel();
                            BlockIns.Add(new CilInstruction(CilOpCodes.Br, simpleBrLabel));
                            destAddr = localsStore[operand1];
                            simpleBrStack.Push((destAddr, simpleBrLabel));
                            break;
                        default:
                            throw new Exception($"Unknown VM Opcode (0x{opcode:x2})");
                    }
                    i++;
                }
                MethodBlocks.Add(_currentBlockAddr, BlockIns);
                newIns.AddRange(BlockIns);
                _currentBlockLabel.Instruction = BlockIns[0];
            }

            return newIns;


        }

    }
}