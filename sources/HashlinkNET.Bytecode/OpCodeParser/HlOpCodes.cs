using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using OpCodeKind = HashlinkNET.Bytecode.HlOpcodeKind;

namespace HashlinkNET.Bytecode.OpCodeParser
{
    public static class HlOpCodes
    {
        public readonly static HlOpCode OMov = new(
            OpCodeKind.Mov,
            [
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
                HlOpCode.PayloadKind.Register
            ]);
        public readonly static HlOpCode OInt = new(
            OpCodeKind.Int,
            [
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
                HlOpCode.PayloadKind.IntIndex
            ]);
        public readonly static HlOpCode OFloat = new(
            OpCodeKind.Float,
            [
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
                HlOpCode.PayloadKind.FloatIndex
            ]);
        public readonly static HlOpCode OBool = new(
            OpCodeKind.Bool,
            [
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
                HlOpCode.PayloadKind.Impl
            ]);
        public readonly static HlOpCode OBytes = new(
            OpCodeKind.Bytes,
            [
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
                HlOpCode.PayloadKind.BytesIndex
            ]);
        public readonly static HlOpCode OString = new(
            OpCodeKind.String,
            [
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
                HlOpCode.PayloadKind.StringIndex
            ]);
        public readonly static HlOpCode ONull = new(
            OpCodeKind.Null,
            [
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult
            ]);

        public readonly static HlOpCode OAdd = new(
            OpCodeKind.Add,
            [
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register
            ]
            );
        public readonly static HlOpCode OSub = new(
            OpCodeKind.Sub,
            [
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register
            ]
            );
        public readonly static HlOpCode OMul = new(
            OpCodeKind.Mul,
            [
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register
            ]
            );
        public readonly static HlOpCode OSDiv = new(
            OpCodeKind.SDiv,
            [
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register
            ]
            );
        public readonly static HlOpCode OUDiv = new(
            OpCodeKind.UDiv,
            [
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register
            ]
            );
        public readonly static HlOpCode OSMod = new(
            OpCodeKind.SMod,
            [
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register
            ]
            );
        public readonly static HlOpCode OUMod = new(
            OpCodeKind.UMod,
            [
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register
            ]
            );
        public readonly static HlOpCode OShl = new(
            OpCodeKind.Shl,
            [
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register
            ]
            );
        public readonly static HlOpCode OSShr = new(
            OpCodeKind.SShr,
            [
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register
            ]
            );
        public readonly static HlOpCode OUShr = new(
            OpCodeKind.UShr,
            [
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register
            ]
            );
        public readonly static HlOpCode OAnd = new(
            OpCodeKind.And,
            [
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register
            ]
            );
        public readonly static HlOpCode OOr = new(
            OpCodeKind.Or,
            [
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register
            ]
            );
        public readonly static HlOpCode OXor = new(
            OpCodeKind.Xor,
            [
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register
            ]
            );

        public readonly static HlOpCode ONeg = new(
            OpCodeKind.Neg,
            [
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
                HlOpCode.PayloadKind.Register
            ]
            );
        public readonly static HlOpCode ONot = new(
            OpCodeKind.Not,
            [
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
                HlOpCode.PayloadKind.Register
            ]
            );
        public readonly static HlOpCode OIncr = new(
            OpCodeKind.Incr,
            [
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
                HlOpCode.PayloadKind.Register
            ]
            );
        public readonly static HlOpCode ODecr = new(
            OpCodeKind.Decr,
            [
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
                HlOpCode.PayloadKind.Register
            ]
            );

        public readonly static HlOpCode OCall0 = new(
            OpCodeKind.Call0,
            [
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
                HlOpCode.PayloadKind.Function
            ]
            );
        public readonly static HlOpCode OCall1 = new(
            OpCodeKind.Call1,
            [
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
                HlOpCode.PayloadKind.Function,
                HlOpCode.PayloadKind.Register
            ]
            );
        public readonly static HlOpCode OCall2 = new(
            OpCodeKind.Call2,
            [
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
                HlOpCode.PayloadKind.Function,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.ExtraParamPointer,
            ]
            );
        public readonly static HlOpCode OCall3 = new(
            OpCodeKind.Call3,
            [
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
                HlOpCode.PayloadKind.Function,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register
            ]
            );
        public readonly static HlOpCode OCall4 = new(
            OpCodeKind.Call4,
            [
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
                HlOpCode.PayloadKind.Function,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register
            ]
            );
        public readonly static HlOpCode OCallN = new(
           OpCodeKind.CallN,
           [
               HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
               HlOpCode.PayloadKind.Function,
               HlOpCode.PayloadKind.VariableCount
           ],
           HlOpCode.PayloadKind.Register
           );
        public readonly static HlOpCode OCallMethod = new(
           OpCodeKind.CallMethod,
           [
               HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
               HlOpCode.PayloadKind.Proto,
               HlOpCode.PayloadKind.VariableCount,
               HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.TypeProvider,
           ],
           HlOpCode.PayloadKind.Register
           );
        public readonly static HlOpCode OCallThis = new(
           OpCodeKind.CallThis,
           [
               HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
               HlOpCode.PayloadKind.Proto | HlOpCode.PayloadKind.DeclaringOnThis,
               HlOpCode.PayloadKind.VariableCount,
           ],
           HlOpCode.PayloadKind.Register
           );
        public readonly static HlOpCode OCallClosure = new(
           OpCodeKind.CallClosure,
           [
               HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.VariableCount
           ],
           HlOpCode.PayloadKind.Register
           );

        public readonly static HlOpCode OStaticClosure = new(
            OpCodeKind.StaticClosure,
            [
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
                HlOpCode.PayloadKind.Function
            ]
            );
        public readonly static HlOpCode OInstanceClosure = new(
            OpCodeKind.InstanceClosure,
            [
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
                HlOpCode.PayloadKind.Function,
                HlOpCode.PayloadKind.Register
            ]
            );
        public readonly static HlOpCode OVirtualClosure = new(
            OpCodeKind.VirtualClosure,
            [
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
                HlOpCode.PayloadKind.Proto,
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.TypeProvider
            ]
            );

        public readonly static HlOpCode OGetGlobal = new(
            OpCodeKind.GetGlobal,
            [
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
                HlOpCode.PayloadKind.GlobalIndex
            ]
            );
        public readonly static HlOpCode OSetGlobal = new(
            OpCodeKind.SetGlobal,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.GlobalIndex
            ]
            );
        public readonly static HlOpCode OField = new(
            OpCodeKind.Field,
            [
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.TypeProvider,
                HlOpCode.PayloadKind.Field,
            ]
            );
        public readonly static HlOpCode OSetField = new(
            OpCodeKind.SetField,
            [
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.TypeProvider,
                HlOpCode.PayloadKind.Field,
                HlOpCode.PayloadKind.Register
            ]
            );
        public readonly static HlOpCode OGetThis = new(
            OpCodeKind.GetThis,
            [
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
                HlOpCode.PayloadKind.Field | HlOpCode.PayloadKind.DeclaringOnThis
            ]
            );
        public readonly static HlOpCode OSetThis = new(
            OpCodeKind.SetThis,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Field | HlOpCode.PayloadKind.DeclaringOnThis
            ]
            );
        public readonly static HlOpCode ODynGet = new(
            OpCodeKind.DynGet,
            [
                HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.StringIndex
            ]
            );
        public readonly static HlOpCode ODynSet = new(
            OpCodeKind.DynSet,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.StringIndex,
                HlOpCode.PayloadKind.Register
            ]
            );

        public readonly static HlOpCode OJTrue = new(
            OpCodeKind.JTrue,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Offset
            ]
            );
        public readonly static HlOpCode OJFalse = new(
            OpCodeKind.JFalse,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Offset
            ]
            );
        public readonly static HlOpCode OJNull = new(
            OpCodeKind.JNull,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Offset
            ]
            );
        public readonly static HlOpCode OJNotNull = new(
            OpCodeKind.JNotNull,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Offset
            ]
            );
        public readonly static HlOpCode OJSLt = new(
            OpCodeKind.JSLt,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Offset
            ]
            );
        public readonly static HlOpCode OJSGte = new(
            OpCodeKind.JSGte,
            [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Offset
            ]
            );
        public readonly static HlOpCode OJSGt = new(
           OpCodeKind.JSGt,
           [
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Offset
           ]
           );
        public readonly static HlOpCode OJSLte = new(
           OpCodeKind.JSLte,
           [
               HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Offset
           ]
           );
        public readonly static HlOpCode OJULt = new(
           OpCodeKind.JULt,
           [
               HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Offset
           ]
           );
        public readonly static HlOpCode OJUGte = new(
           OpCodeKind.JUGte,
           [
               HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Offset
           ]
           );
        public readonly static HlOpCode OJNotLt = new(
           OpCodeKind.JNotLt,
           [
               HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Offset
           ]
           );
        public readonly static HlOpCode OJNotGte = new(
           OpCodeKind.JNotGte,
           [
               HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Offset
           ]
           );
        public readonly static HlOpCode OJEq = new(
           OpCodeKind.JEq,
           [
               HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Offset
           ]
           );
        public readonly static HlOpCode OJNotEq = new(
           OpCodeKind.JNotEq,
           [
               HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Register,
                HlOpCode.PayloadKind.Offset
           ]
           );
        public readonly static HlOpCode OJAlways = new(
           OpCodeKind.JAlways,
           [
                HlOpCode.PayloadKind.Offset
           ]
           );

        public readonly static HlOpCode OToDyn = new(
           OpCodeKind.ToDyn,
           [
               HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
               HlOpCode.PayloadKind.Register,
           ]
           );
        public readonly static HlOpCode OToSFloat = new(
           OpCodeKind.ToSFloat,
           [
               HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
               HlOpCode.PayloadKind.Register,
           ]
           );
        public readonly static HlOpCode OToUFloat = new(
           OpCodeKind.ToUFloat,
           [
               HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
               HlOpCode.PayloadKind.Register,
           ]
           );
        public readonly static HlOpCode OToInt = new(
           OpCodeKind.ToInt,
           [
               HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
               HlOpCode.PayloadKind.Register,
           ]
           );
        public readonly static HlOpCode OSafeCast = new(
           OpCodeKind.SafeCast,
           [
               HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
               HlOpCode.PayloadKind.Register,
           ]
           );
        public readonly static HlOpCode OUnsafeCast = new(
           OpCodeKind.UnsafeCast,
           [
               HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
               HlOpCode.PayloadKind.Register,
           ]
           );
        public readonly static HlOpCode OToVirtual = new(
           OpCodeKind.ToVirtual,
           [
               HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
               HlOpCode.PayloadKind.Register,
           ]
           );

        public readonly static HlOpCode OLabel = new(
           OpCodeKind.Label,
           [
           ]
           );
        public readonly static HlOpCode ORet = new(
           OpCodeKind.Ret,
           [
               HlOpCode.PayloadKind.Register
           ]
           );
        public readonly static HlOpCode OThrow = new(
           OpCodeKind.Throw,
           [
               HlOpCode.PayloadKind.Register
           ]
           );
        public readonly static HlOpCode ORethrow = new(
           OpCodeKind.Rethrow,
           [
           ]
           );
        public readonly static HlOpCode OSwitch = new(
           OpCodeKind.Switch,
           [
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.VariableCount,
               HlOpCode.PayloadKind.Register
           ],
           HlOpCode.PayloadKind.Offset
           );
        public readonly static HlOpCode ONullCheck = new(
           OpCodeKind.NullCheck,
           [
               HlOpCode.PayloadKind.Register
           ]
           );
        public readonly static HlOpCode OTrap = new(
           OpCodeKind.Trap,
           [
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Offset
           ]
           );
        public readonly static HlOpCode OEndTrap = new(
           OpCodeKind.EndTrap,
           [
               HlOpCode.PayloadKind.Register
           ]
           );

        public readonly static HlOpCode OGetI8 = new(
           OpCodeKind.GetI8,
           [
               HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Register,
           ]
           );
        public readonly static HlOpCode OGetI16 = new(
           OpCodeKind.GetI16,
           [
               HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Register,
           ]
           );
        public readonly static HlOpCode OGetMem = new(
           OpCodeKind.GetMem,
           [
               HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Register,
           ]
           );
        public readonly static HlOpCode OGetArray = new(
           OpCodeKind.GetArray,
           [
               HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Register,
           ]
           );
        public readonly static HlOpCode OSetI8 = new(
           OpCodeKind.SetI8,
           [
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Register,
           ]
           );
        public readonly static HlOpCode OSetI16 = new(
           OpCodeKind.SetI16,
           [
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Register,
           ]
           );
        public readonly static HlOpCode OSetMem = new(
           OpCodeKind.SetMem,
           [
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Register,
           ]
           );
        public readonly static HlOpCode OSetArray = new(
           OpCodeKind.SetArray,
           [
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Register,
               HlOpCode.PayloadKind.Register,
           ]
           );

        public readonly static HlOpCode ONew = new(
           OpCodeKind.New,
           [
               HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.TypeProvider | HlOpCode.PayloadKind.StoreResult,
           ]
           );
        public readonly static HlOpCode OArraySize = new(
           OpCodeKind.ArraySize,
           [
               HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
               HlOpCode.PayloadKind.Register
           ]
           );
        public readonly static HlOpCode OType = new(
           OpCodeKind.Type,
           [
               HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
               HlOpCode.PayloadKind.Type
           ]
           );
        public readonly static HlOpCode OGetType = new(
           OpCodeKind.GetType,
           [
               HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
               HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.TypeProvider
           ]
           );
        public readonly static HlOpCode OGetTID = new(
          OpCodeKind.GetTID,
          [
              HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
              HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.TypeProvider
          ]
          );

        public readonly static HlOpCode ORef = new(
          OpCodeKind.Ref,
          [
              HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
               HlOpCode.PayloadKind.Register
          ]
          );
        public readonly static HlOpCode OUnref = new(
          OpCodeKind.Unref,
          [
              HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
               HlOpCode.PayloadKind.Register
          ]
          );
        public readonly static HlOpCode OSetref = new(
          OpCodeKind.Setref,
          [
              HlOpCode.PayloadKind.Register,
              HlOpCode.PayloadKind.Register
          ]
          );

        public readonly static HlOpCode OMakeEnum = new(
          OpCodeKind.MakeEnum,
          [
              HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.TypeProvider | HlOpCode.PayloadKind.StoreResult,
              HlOpCode.PayloadKind.Impl,
              HlOpCode.PayloadKind.VariableCount
          ],
          HlOpCode.PayloadKind.Register
          );
        public readonly static HlOpCode OEnumAlloc = new(
          OpCodeKind.EnumAlloc,
          [
              HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.TypeProvider | HlOpCode.PayloadKind.StoreResult,
              HlOpCode.PayloadKind.Impl
          ]
          );
        public readonly static HlOpCode OEnumIndex = new(
          OpCodeKind.EnumIndex,
          [
              HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
              HlOpCode.PayloadKind.Register
          ]
          );
        public readonly static HlOpCode OEnumField = new(
          OpCodeKind.EnumField,
          [
              HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
              HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.TypeProvider,
              HlOpCode.PayloadKind.Impl,
              HlOpCode.PayloadKind.EnumFieldIndex | HlOpCode.PayloadKind.ExtraParamPointer
          ]
          );
        public readonly static HlOpCode OSetEnumField = new(
          OpCodeKind.SetEnumField,
          [
              HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.TypeProvider,
              HlOpCode.PayloadKind.EnumFieldIndex,
              HlOpCode.PayloadKind.Register
          ]
          );

        public readonly static HlOpCode OAssert = new(
          OpCodeKind.Assert,
          [
          ]
          );
        public readonly static HlOpCode ORefData = new(
          OpCodeKind.RefData,
          [
              HlOpCode.PayloadKind.Register,
              HlOpCode.PayloadKind.Register
          ]
          );
        public readonly static HlOpCode ORefOffset = new(
         OpCodeKind.RefOffset,
         [
             HlOpCode.PayloadKind.Register | HlOpCode.PayloadKind.StoreResult,
             HlOpCode.PayloadKind.Register,
             HlOpCode.PayloadKind.Register
         ]
         );
        public readonly static HlOpCode ONop = new(
         OpCodeKind.Nop,
         [
         ]
         );

        public static IList<HlOpCode> OpCodes
        {
            get;
        }

        static HlOpCodes()
        {
            var opcodes = new HlOpCode[(int)OpCodeKind.Last + 1];
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
