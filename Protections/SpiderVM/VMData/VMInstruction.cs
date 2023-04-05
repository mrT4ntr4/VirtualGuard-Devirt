using System;
using System.IO;

namespace VirtualGuardDevirt.Protections.SpiderVM.VMData
{
    public class VMInstruction
    {
        public byte Opcode { get; set; }
        public dynamic Operand { get; set; }

        public static dynamic ReadOperand(BinaryReader _reader)
        {
            int operandType = _reader.ReadInt32();
            dynamic operand = null;

            switch (operandType)
            {
                case -1:
                    operand = _reader.ReadBoolean();
                    break;
                case 0:
                    break;
                case 1:
                    operand = _reader.ReadInt16();
                    break;
                case 2:
                    operand = _reader.ReadInt32();
                    break;
                case 3:
                    operand = _reader.ReadInt64();
                    break;
                case 4:
                    operand = _reader.ReadUInt16();
                    break;
                case 5:
                    operand = _reader.ReadUInt32();
                    break;
                case 6:
                    operand = _reader.ReadUInt64();
                    break;
                case 7:
                    operand = _reader.ReadSByte();
                    break;
                case 8:
                    operand = _reader.ReadByte();
                    break;
                case 9:
                    operand = _reader.ReadString();
                    break;
                case 0xA:
                    operand = _reader.ReadSingle();
                    break;
                case 0xB:
                    operand = _reader.ReadDouble();
                    break;
                default:
                    throw new InvalidCastException();
            }

            return operand;
        }
    }
}
