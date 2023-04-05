using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using VirtualGuardDevirt.Protections.SpiderVM.VMData;
using static VirtualGuardDevirt.Logger;
using static VirtualGuardDevirt.Context;
using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Cil;

namespace VirtualGuardDevirt.Protections.SpiderVM
{

    public class idk
    {
        public int R1 { get; set; }
        public int R2 { get; set; }
        public int R3 { get; set; }
        public int R4 { get; set; }
    }

    public class ConstructorArgs
    {
        public int A0 { get; set;}
        public int A1 { get; set;}
    }

    internal class InitializeMethod
    {
        public static ConstructorArgs cctorArgs = new ConstructorArgs();

        public static void GetCctorArgs()
        {

            MethodDefinition moduleCtr = module.GetModuleConstructor();
            var instrs = moduleCtr.CilMethodBody.Instructions;
            int instrCount = instrs.Count;
            if (instrs[instrCount - 2].OpCode == CilOpCodes.Call 
                && instrs[instrCount - 2].Operand.ToString().Contains("9::f29")
                && instrs[instrCount - 4].IsLdcI4()
                && instrs[instrCount - 3].IsLdcI4())
            {
                cctorArgs.A0 = instrs[instrCount - 4].GetLdcI4Constant();
                cctorArgs.A1 = instrs[instrCount - 3].GetLdcI4Constant();
            }
            else
            {
                throw new Exception("Failed to find Constructor Args!");
            }
        }

        private static byte XorByte(byte arg)
        {
            return (byte)((int)arg ^ cctorArgs.A0);
        }

        internal static void InitiliaseMethodage()
        {
            Log("Finding Constructor Args", TypeMessage.Debug);
            GetCctorArgs();
            Log($"Constructor Arguments Found : 0x{cctorArgs.A0:x8}, 0x{cctorArgs.A1:x8}", TypeMessage.Debug);


            using (MemoryStream crocStream = new MemoryStream(VM.SpiderBytecode))
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
                            List<idk> idkList = new List<idk>();

                            int num = _reader.ReadInt32();
                            for (int i = 0; i < num; i++)
                            {
                                List<VMInstruction> list = new List<VMInstruction>();
                                int num2 = _reader.ReadInt32();
                                int num3 = _reader.ReadInt32();
                                int key = _reader.ReadInt32();
                                for (int j = 0; j < num2; j++)
                                {
                                    idk item = new idk
                                    {
                                        R1 = _reader.ReadInt32(),
                                        R2 = _reader.ReadInt32(),
                                        R3 = _reader.ReadInt32(),
                                        R4 = _reader.ReadInt32()
                                    };
                                    idkList.Add(item);
                                }

                                for (int k = 0; k < num3; k++)
                                {
                                    byte opcode = (byte)((int)_reader.ReadByte() ^ cctorArgs.A1);

                                    list.Add(new VMInstruction
                                    {
                                        Opcode = opcode,
                                        Operand = VMInstruction.ReadOperand(_reader)
                                    });
                                }
                                VM.Methods.Add(key, list);
                            }
                        }
                    }
                }
            }
        }


    }
}