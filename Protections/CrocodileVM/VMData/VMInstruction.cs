using System;
using System.IO;

namespace VirtualGuardDevirt.Protections.CrocodileVM.VMData
{
    public class VMInstruction
    {
        public byte Opcode { get; set; }
        public int Operand1 { get; set; }
        public dynamic Operand2 { get; set; }

        public static dynamic ReadOperand(BinaryReader _reader)
        {
            int operandType = _reader.ReadInt32();
            dynamic operand;

            switch (operandType)
            {
                case 0:
                    operand = _reader.ReadInt32();
                    break;
                case 1:
                    operand = _reader.ReadInt64();
                    break;
                case 2:
                    operand = _reader.ReadString();
                    break;
                default:
                    operand = -1;
                    break;
            }
            return operand;
        }
    }
}
