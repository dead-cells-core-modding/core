using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Patch
{
    public class HlOpCode(
        HL_opcode.OpCodes kind,
        HlOpCode.PayloadKind[] payloads,
        HlOpCode.PayloadKind? variablePayload = null)
    {
        [Flags]
        public enum PayloadKind
        {
            None = 0,
            VariableCount = 1,

            Register = 2,
            Type = 4,
            Function = 8,
            Offset = 16,

            Impl = 32,
            IntIndex = 64 | IndexedConstants,
            FloatIndex = 128 | IndexedConstants,
            StringIndex = 256 | IndexedConstants,
            BytesIndex = 512 | IndexedConstants,
            GlobalIndex = 1024,

            EnumFieldIndex = 2048,
            //Others
            Field = 4096 | RequestTypeInfo,
            Proto = 8192 | RequestTypeInfo,

            //Flags
            TypeProvider = 1 << 16,
            DeclaringOnThis = 1 << 17,
            RequestTypeInfo = 1 << 18,
            IndexedConstants = 1 << 19,
            ExtraParamPointer = 1 << 20,
        }
        public HL_opcode.OpCodes OpCode
        {
            get;
        } = kind;
        public PayloadKind[] Payloads
        {
            get;
        } = payloads;
        public PayloadKind? VariablePayload
        {
            get;
        } = variablePayload;

        public override int GetHashCode()
        {
            return OpCode.GetHashCode();
        }
        public override bool Equals( object? obj )
        {
            return obj is HlOpCode op && op.OpCode == OpCode;
        }

        public override string ToString()
        {
            return OpCode.ToString();
        }
        public static bool operator ==( HlOpCode lhs, HlOpCode rhs )
        {
            return lhs.OpCode == rhs.OpCode;
        }
        public static bool operator !=( HlOpCode lhs, HlOpCode rhs )
        {
            return lhs.OpCode != rhs.OpCode;
        }
    }
}
