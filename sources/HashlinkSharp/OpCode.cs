using System.Runtime.InteropServices;

namespace Hashlink
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct HL_opcode
    {
        public enum OpCodes
        {
            OMov,
            OInt,
            OFloat,
            OBool,
            OBytes,
            OString,
            ONull,

            OAdd,
            OSub,
            OMul,
            OSDiv,
            OUDiv,
            OSMod,
            OUMod,
            OShl,
            OSShr,
            OUShr,
            OAnd,
            OOr,
            OXor,

            ONeg,
            ONot,
            OIncr,
            ODecr,

            OCall0,
            OCall1,
            OCall2,
            OCall3,
            OCall4,
            OCallN,
            OCallMethod,
            OCallThis,
            OCallClosure,

            OStaticClosure,
            OInstanceClosure,
            OVirtualClosure,

            OGetGlobal,
            OSetGlobal,
            OField,
            OSetField,
            OGetThis,
            OSetThis,
            ODynGet,
            ODynSet,

            OJTrue,
            OJFalse,
            OJNull,
            OJNotNull,
            OJSLt,
            OJSGte,
            OJSGt,
            OJSLte,
            OJULt,
            OJUGte,
            OJNotLt,
            OJNotGte,
            OJEq,
            OJNotEq,
            OJAlways,

            OToDyn,
            OToSFloat,
            OToUFloat,
            OToInt,
            OSafeCast,
            OUnsafeCast,
            OToVirtual,

            OLabel,
            ORet,
            OThrow,
            ORethrow,
            OSwitch,
            ONullCheck,
            OTrap,
            OEndTrap,

            OGetI8,
            OGetI16,
            OGetMem,
            OGetArray,
            OSetI8,
            OSetI16,
            OSetMem,
            OSetArray,

            ONew,
            OArraySize,
            OType,
            OGetType,
            OGetTID,

            ORef,
            OUnref,
            OSetref,

            OMakeEnum,
            OEnumAlloc,
            OEnumIndex,
            OEnumField,
            OSetEnumField,

            OAssert,
            ORefData,
            ORefOffset,
            ONop,
            //  
            OLast,
            ODCCM_Helper_Start,

            ORealLast
        }
        public OpCodes op;
        public int p1;
        public int p2;
        public int p3;
        public int* extra;
    }
}
