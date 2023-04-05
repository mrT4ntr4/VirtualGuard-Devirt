using AsmResolver.DotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using VirtualGuardDevirt.Protections.CrocodileVM.VMData;
using static VirtualGuardDevirt.Context;
using static VirtualGuardDevirt.Logger;

namespace VirtualGuardDevirt.Protections.CrocodileVM
{
    internal class InitializeMethod
    {
        public static int cctorArg = 0;
        public static int GetCcctorArg()
        {
            MethodDefinition moduleCtr = module.GetModuleConstructor();
            var instrs = moduleCtr.CilMethodBody.Instructions;
            if (instrs.Count == 3)
            {
                if (instrs[0].IsLdcI4())
                {
                    return instrs[0].GetLdcI4Constant();
                }
                else
                {
                    throw new Exception("Failed to find Constructor Arg!");
                }
            }
            return 0;
        }
        private static byte XorByte(byte arg)
        {
            return (byte)((int)arg ^ cctorArg);
        }
        internal static void InitiliaseMethodage()
        {
            Log("Finding Disassembly Constant", TypeMessage.Debug);
            cctorArg = GetCcctorArg();
            Log($"Disassembly Constant Found : 0x{cctorArg:x8}", TypeMessage.Debug);

            using (MemoryStream crocStream = new MemoryStream(VM.CrocodileBytecode))
            {
                using (DeflateStream deflateStream = new DeflateStream(crocStream, CompressionMode.Decompress))
                {
                    byte[] source;
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        deflateStream.CopyTo(memoryStream);
                        source = memoryStream.ToArray();
                    }
                    using (MemoryStream input = new MemoryStream(source.Select(new Func<byte, byte>(XorByte)).ToArray<byte>()))
                    {
                        using (BinaryReader _reader = new BinaryReader(input))
                        {
                            int numBlocks = _reader.ReadInt32() ^ cctorArg;

                            List<int> blockTokens = new List<int>();

                            for (int i = 0; i < numBlocks; i++)
                            {
                                List<VMInstruction> blockInstructions = new List<VMInstruction>();

                                int token = _reader.ReadInt32() ^ numBlocks;
                                int numInstr = _reader.ReadInt32() ^ numBlocks;

                                for (int j = 0; j < numInstr; j++)
                                {
                                    VMInstruction vmInstr = new VMInstruction
                                    {
                                        Opcode = _reader.ReadByte(),
                                        Operand1 = VMInstruction.ReadOperand(_reader),
                                        Operand2 = VMInstruction.ReadOperand(_reader),
                                    };
                                    blockInstructions.Add(vmInstr);
                                }

                                VM.Blocks.Add(token, blockInstructions);
                            }
                        }
                    }
                }
            }
        }
    }
}