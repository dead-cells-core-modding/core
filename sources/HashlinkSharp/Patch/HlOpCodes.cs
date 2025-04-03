using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Patch
{
    public static class HlOpCodes
    {
        public readonly static HlOpCode OMov = new(
            HL_opcode.OpCodes.OMov,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register
            ]);
        public readonly static HlOpCode OInt = new(
            HL_opcode.OpCodes.OInt,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.IntIndex
            ]);
        public readonly static HlOpCode OFloat = new(
            HL_opcode.OpCodes.OFloat,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.FloatIndex
            ]);
        public readonly static HlOpCode OBool = new(
            HL_opcode.OpCodes.OBool,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Impl
            ]);
        public readonly static HlOpCode OBytes = new(
            HL_opcode.OpCodes.OBytes,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.BytesIndex
            ]);
        public readonly static HlOpCode OString = new(
            HL_opcode.OpCodes.OString,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.StringIndex
            ]);
        public readonly static HlOpCode ONull = new(
            HL_opcode.OpCodes.ONull,
            [
                HlOpCode.PayloadKind.Register
            ]);

        public readonly static HlOpCode OAdd = new(
            HL_opcode.OpCodes.OAdd,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register
            ]
            );
        public readonly static HlOpCode OSub = new(
            HL_opcode.OpCodes.OSub,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register
            ]
            );
        public readonly static HlOpCode OMul = new(
            HL_opcode.OpCodes.OMul,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register
            ]
            );
        public readonly static HlOpCode OSDiv = new(
            HL_opcode.OpCodes.OSDiv,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register
            ]
            );
        public readonly static HlOpCode OUDiv = new(
            HL_opcode.OpCodes.OUDiv,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register
            ]
            );
        public readonly static HlOpCode OSMod = new(
            HL_opcode.OpCodes.OSMod,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register
            ]
            );
        public readonly static HlOpCode OUMod = new(
            HL_opcode.OpCodes.OUMod,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register
            ]
            );
        public readonly static HlOpCode OShl = new(
            HL_opcode.OpCodes.OShl,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register
            ]
            );
        public readonly static HlOpCode OSShr = new(
            HL_opcode.OpCodes.OSShr,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register
            ]
            );
        public readonly static HlOpCode OUShr = new(
            HL_opcode.OpCodes.OUShr,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register
            ]
            );
        public readonly static HlOpCode OAnd = new(
            HL_opcode.OpCodes.OAnd,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register
            ]
            );
        public readonly static HlOpCode OOr = new(
            HL_opcode.OpCodes.OOr,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register
            ]
            );
        public readonly static HlOpCode OXor = new(
            HL_opcode.OpCodes.OXor,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register
            ]
            );

        public readonly static HlOpCode ONeg = new(
            HL_opcode.OpCodes.ONeg,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register
            ]
            );
        public readonly static HlOpCode ONot = new(
            HL_opcode.OpCodes.ONot,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register
            ]
            );
        public readonly static HlOpCode OIncr = new(
            HL_opcode.OpCodes.OIncr,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register
            ]
            );
        public readonly static HlOpCode ODecr = new(
            HL_opcode.OpCodes.ODecr,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register
            ]
            );

        public readonly static HlOpCode OCall0 = new(
            HL_opcode.OpCodes.OCall0,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Function
            ]
            );
        public readonly static HlOpCode OCall1 = new(
            HL_opcode.OpCodes.OCall1,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Function,
                HlOpCode.PayloadKind.Register
            ]
            );
        public readonly static HlOpCode OCall2 = new(
            HL_opcode.OpCodes.OCall2,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Function,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register,
            ]
            );
        public readonly static HlOpCode OCall3 = new(
            HL_opcode.OpCodes.OCall3,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Function,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register
            ]
            );
        public readonly static HlOpCode OCall4 = new(
            HL_opcode.OpCodes.OCall4,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Function,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register
            ]
            );
        public readonly static HlOpCode OCallN = new(
           HL_opcode.OpCodes.OCallN,
           [
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Function,
               HlOpCode.PayloadKind.VariableCount
           ],
           HlOpCode.PayloadKind.Register
           );
        public readonly static HlOpCode OCallMethod = new(
           HL_opcode.OpCodes.OCallMethod,
           [
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Proto,
               HlOpCode.PayloadKind.VariableCount,
               HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.TypeProvider,
           ],
           HlOpCode.PayloadKind.Register
           );
        public readonly static HlOpCode OCallThis = new(
           HL_opcode.OpCodes.OCallThis,
           [
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Proto | HlOpCode.PayloadKind.DeclaringOnThis,
               HlOpCode.PayloadKind.VariableCount,
           ],
           HlOpCode.PayloadKind.Register
           );
        public readonly static HlOpCode OCallClosure = new(
           HL_opcode.OpCodes.OCallClosure,
           [
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.VariableCount
           ],
           HlOpCode.PayloadKind.Register
           );

        public readonly static HlOpCode OStaticClosure = new(
            HL_opcode.OpCodes.OStaticClosure,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Function
            ]
            );
        public readonly static HlOpCode OInstanceClosure = new(
            HL_opcode.OpCodes.OInstanceClosure,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Function,
                HlOpCode.PayloadKind.Register
            ]
            );
        public readonly static HlOpCode OVirtualClosure = new(
            HL_opcode.OpCodes.OVirtualClosure,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Proto,
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.TypeProvider
            ]
            );

        public readonly static HlOpCode OGetGlobal = new(
            HL_opcode.OpCodes.OGetGlobal,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.GlobalIndex
            ]
            );
        public readonly static HlOpCode OSetGlobal = new(
            HL_opcode.OpCodes.OSetGlobal,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.GlobalIndex
            ]
            );
        public readonly static HlOpCode OField = new(
            HL_opcode.OpCodes.OField,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.TypeProvider,
                HlOpCode.PayloadKind.Field,
            ]
            );
        public readonly static HlOpCode OSetField = new(
            HL_opcode.OpCodes.OSetField,
            [
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.TypeProvider,
                HlOpCode.PayloadKind.Field,
                HlOpCode.PayloadKind.Register
            ]
            );
        public readonly static HlOpCode OGetThis = new(
            HL_opcode.OpCodes.OGetThis,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Field | HlOpCode.PayloadKind.DeclaringOnThis
            ]
            );
        public readonly static HlOpCode OSetThis = new(
            HL_opcode.OpCodes.OSetThis,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Field | HlOpCode.PayloadKind.DeclaringOnThis
            ]
            );
        public readonly static HlOpCode ODynGet = new(
            HL_opcode.OpCodes.ODynGet,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.StringIndex
            ]
            );
        public readonly static HlOpCode ODynSet = new(
            HL_opcode.OpCodes.ODynSet,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.StringIndex,
                HlOpCode.PayloadKind.Register
            ]
            );

        public readonly static HlOpCode OJTrue = new(
            HL_opcode.OpCodes.OJTrue,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Offset
            ]
            );
        public readonly static HlOpCode OJFalse = new(
            HL_opcode.OpCodes.OJFalse,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Offset
            ]
            );
        public readonly static HlOpCode OJNull = new(
            HL_opcode.OpCodes.OJNull,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Offset
            ]
            );
        public readonly static HlOpCode OJNotNull = new(
            HL_opcode.OpCodes.OJNotNull,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Offset
            ]
            );
        public readonly static HlOpCode OJSLt = new(
            HL_opcode.OpCodes.OJSLt,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Offset
            ]
            );
        public readonly static HlOpCode OJSGte = new(
            HL_opcode.OpCodes.OJSGte,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Offset
            ]
            );
        public readonly static HlOpCode OJSGt = new(
           HL_opcode.OpCodes.OJSGt,
           [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Offset
           ]
           );
        public readonly static HlOpCode OJSLte = new(
           HL_opcode.OpCodes.OJSLte,
           [
               HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Offset
           ]
           );
        public readonly static HlOpCode OJULt = new(
           HL_opcode.OpCodes.OJULt,
           [
               HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Offset
           ]
           );
        public readonly static HlOpCode OJUGte = new(
           HL_opcode.OpCodes.OJUGte,
           [
               HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Offset
           ]
           );
        public readonly static HlOpCode OJNotLt = new(
           HL_opcode.OpCodes.OJNotLt,
           [
               HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Offset
           ]
           );
        public readonly static HlOpCode OJNotGte = new(
           HL_opcode.OpCodes.OJNotGte,
           [
               HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Offset
           ]
           );
        public readonly static HlOpCode OJEq = new(
           HL_opcode.OpCodes.OJEq,
           [
               HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Offset
           ]
           );
        public readonly static HlOpCode OJNotEq = new(
           HL_opcode.OpCodes.OJNotEq,
           [
               HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Offset
           ]
           );
        public readonly static HlOpCode OJAlways = new(
           HL_opcode.OpCodes.OJAlways,
           [
                HlOpCode.PayloadKind.Offset
           ]
           );

        public readonly static HlOpCode OToDyn = new(
           HL_opcode.OpCodes.OToDyn,
           [
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Register,
           ]
           );
        public readonly static HlOpCode OToSFloat = new(
           HL_opcode.OpCodes.OToSFloat,
           [
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Register,
           ]
           );
        public readonly static HlOpCode OToUFloat = new(
           HL_opcode.OpCodes.OToUFloat,
           [
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Register,
           ]
           );
        public readonly static HlOpCode OToInt = new(
           HL_opcode.OpCodes.OToInt,
           [
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Register,
           ]
           );
        public readonly static HlOpCode OSafeCast = new(
           HL_opcode.OpCodes.OSafeCast,
           [
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Register,
           ]
           );
        public readonly static HlOpCode OUnsafeCast = new(
           HL_opcode.OpCodes.OUnsafeCast,
           [
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Register,
           ]
           );
        public readonly static HlOpCode OToVirtual = new(
           HL_opcode.OpCodes.OToVirtual,
           [
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Register,
           ]
           );

        public readonly static HlOpCode OLabel = new(
           HL_opcode.OpCodes.OLabel,
           [
           ]
           );
        public readonly static HlOpCode ORet = new(
           HL_opcode.OpCodes.ORet,
           [
               HlOpCode.PayloadKind.Register
           ]
           );
        public readonly static HlOpCode OThrow = new(
           HL_opcode.OpCodes.OThrow,
           [
               HlOpCode.PayloadKind.Register
           ]
           );
        public readonly static HlOpCode ORethrow = new(
           HL_opcode.OpCodes.ORethrow,
           [
           ]
           );
        public readonly static HlOpCode OSwitch = new(
           HL_opcode.OpCodes.OSwitch,
           [
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.VariableCount,
               HlOpCode.PayloadKind.Register
           ],
           HlOpCode.PayloadKind.Offset
           );
        public readonly static HlOpCode ONullCheck = new(
           HL_opcode.OpCodes.ONullCheck,
           [
               HlOpCode.PayloadKind.Register
           ]
           );
        public readonly static HlOpCode OTrap = new(
           HL_opcode.OpCodes.OTrap,
           [
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Offset
           ]
           );
        public readonly static HlOpCode OEndTrap = new(
           HL_opcode.OpCodes.OEndTrap,
           [
               HlOpCode.PayloadKind.Register
           ]
           );

        public readonly static HlOpCode OGetI8 = new(
           HL_opcode.OpCodes.OGetI8,
           [
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Register,
           ]
           );
        public readonly static HlOpCode OGetI16 = new(
           HL_opcode.OpCodes.OGetI16,
           [
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Register,
           ]
           );
        public readonly static HlOpCode OGetMem = new(
           HL_opcode.OpCodes.OGetMem,
           [
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Register,
           ]
           );
        public readonly static HlOpCode OGetArray = new(
           HL_opcode.OpCodes.OGetArray,
           [
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Register,
           ]
           );
        public readonly static HlOpCode OSetI8 = new(
           HL_opcode.OpCodes.OGetI8,
           [
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Register,
           ]
           );
        public readonly static HlOpCode OSetI16 = new(
           HL_opcode.OpCodes.OGetI16,
           [
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Register,
           ]
           );
        public readonly static HlOpCode OSetMem = new(
           HL_opcode.OpCodes.OGetMem,
           [
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Register,
           ]
           );
        public readonly static HlOpCode OSetArray = new(
           HL_opcode.OpCodes.OGetArray,
           [
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Register,
           ]
           );

        public readonly static HlOpCode ONew = new(
           HL_opcode.OpCodes.ONew,
           [
               HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.TypeProvider,
           ]
           );
        public readonly static HlOpCode OArraySize = new(
           HL_opcode.OpCodes.OArraySize,
           [
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Register
           ]
           );
        public readonly static HlOpCode OType = new(
           HL_opcode.OpCodes.OType,
           [
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Type
           ]
           );
        public readonly static HlOpCode OGetType = new(
           HL_opcode.OpCodes.OGetType,
           [
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.TypeProvider
           ]
           );
        public readonly static HlOpCode OGetTID = new(
          HL_opcode.OpCodes.OGetTID,
          [
              HlOpCode.PayloadKind.Register,
              HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.TypeProvider
          ]
          );

        public readonly static HlOpCode ORef = new(
          HL_opcode.OpCodes.ORef,
          [
              HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Register
          ]
          );
        public readonly static HlOpCode OUnref = new(
          HL_opcode.OpCodes.OUnref,
          [
              HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Register
          ]
          );
        public readonly static HlOpCode OSetref = new(
          HL_opcode.OpCodes.OSetref,
          [
              HlOpCode.PayloadKind.Register,
              HlOpCode.PayloadKind.Register
          ]
          );

        public readonly static HlOpCode OMakeEnum = new(
          HL_opcode.OpCodes.OMakeEnum,
          [
              HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.TypeProvider,
              HlOpCode.PayloadKind.Impl,
              HlOpCode.PayloadKind.VariableCount
          ],
          HlOpCode.PayloadKind.Register
          );
        public readonly static HlOpCode OEnumAlloc = new(
          HL_opcode.OpCodes.OEnumAlloc,
          [
              HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.TypeProvider,
              HlOpCode.PayloadKind.Impl
          ]
          );
        public readonly static HlOpCode OEnumIndex = new(
          HL_opcode.OpCodes.OEnumAlloc,
          [
              HlOpCode.PayloadKind.Register,
              HlOpCode.PayloadKind.Register
          ]
          );
        public readonly static HlOpCode OEnumField = new(
          HL_opcode.OpCodes.OEnumField,
          [
              HlOpCode.PayloadKind.Register,
              HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.TypeProvider,
              HlOpCode.PayloadKind.Impl,
              HlOpCode.PayloadKind.EnumFieldIndex
          ]
          );
        public readonly static HlOpCode OSetEnumField = new(
          HL_opcode.OpCodes.OSetEnumField,
          [
              HlOpCode.PayloadKind.Register,
              HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.TypeProvider,
              HlOpCode.PayloadKind.EnumFieldIndex
          ]
          );

        public readonly static HlOpCode OAssert = new(
          HL_opcode.OpCodes.OAssert,
          [
          ]
          );
        public readonly static HlOpCode ORefData = new(
          HL_opcode.OpCodes.ORefData,
          [
              HlOpCode.PayloadKind.Register,
              HlOpCode.PayloadKind.Register
          ]
          );
        public readonly static HlOpCode ORefOffset = new(
         HL_opcode.OpCodes.ORefOffset,
         [
             HlOpCode.PayloadKind.Register,
             HlOpCode.PayloadKind.Register,
             HlOpCode.PayloadKind.Register
         ]
         );
        public readonly static HlOpCode ONop = new(
         HL_opcode.OpCodes.ONop,
         [
         ]
         );

        public static IList<HlOpCode> OpCodes
        {
            get;
        }

        static HlOpCodes()
        {
            var opcodes = new HlOpCode[(int)HL_opcode.OpCodes.ORealLast + 1];
            foreach (var v in typeof(HlOpCodes).GetRuntimeFields())
            {
                if (v.FieldType == typeof(HlOpCode))
                {
                    var op = (HlOpCode) v.GetValue(null)!;
                    opcodes[(int)op.OpCode] = op;
                }
            }
            OpCodes = ImmutableList.Create(opcodes);
        }
    }
}
