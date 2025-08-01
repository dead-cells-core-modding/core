using HashlinkNET.Bytecode;
using HashlinkNET.Bytecode.OpCodeParser;
using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Data.Interfaces;
using HashlinkNET.Compiler.Pseudocode.Data;
using HashlinkNET.Compiler.Pseudocode.IR;
using HashlinkNET.Compiler.Pseudocode.IR.Array;
using HashlinkNET.Compiler.Pseudocode.IR.Call;
using HashlinkNET.Compiler.Pseudocode.IR.Closure;
using HashlinkNET.Compiler.Pseudocode.IR.DynObj;
using HashlinkNET.Compiler.Pseudocode.IR.EnumOpt;
using HashlinkNET.Compiler.Pseudocode.IR.FlowControl;
using HashlinkNET.Compiler.Pseudocode.IR.Mem;
using HashlinkNET.Compiler.Pseudocode.IR.Object;
using HashlinkNET.Compiler.Pseudocode.IR.Opterators;
using HashlinkNET.Compiler.Pseudocode.IR.Ref;
using HashlinkNET.Compiler.Steps;
using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.Steps
{
    internal class ParseOpCodesStep : CompileStep
    {
        private GlobalData gdata2 = null!;
        private FuncEmitGlobalData gdata = null!;
        private TypeSystem typeSystem = null!;
        private IDataContainer container = null!;
        private string? currentAssign = null;
        public override void Execute( IDataContainer container )
        {
            this.container = container;
            gdata = container.GetGlobalData<FuncEmitGlobalData>();
            gdata2 = container.GetGlobalData<GlobalData>();
            typeSystem = gdata2.Module.TypeSystem;
            var f = gdata.Function;

            var bb = gdata.IRBasicBlocks;

            Dictionary<HlBasicBlockData, IRBasicBlockData> mapping = [];

            ParseBB(gdata.HlBasicBlocks[0]);

            IRBasicBlockData ParseBB( HlBasicBlockData hlbb )
            {
                if (mapping.TryGetValue(hlbb, out var irbb))
                {
                    return irbb;
                }

                irbb = new()
                {
                    startInHlbc = hlbb.opcodeStart,
                    index = bb.Count
                };
                bb.Add(irbb);
                mapping.Add(hlbb, irbb);
                foreach (var v in hlbb.transitions)
                {
                    var bb = ParseBB(v.Target);
                    bb.parents.Add(irbb);
                    if (v.Kind == TransitionKind.Default)
                    {
                        irbb.defaultTransition = bb;
                    }
                    irbb.transitions.Add(
                        new(bb, v.Kind)
                        );
                }

                //Parse OpCodes
                int optIdx = hlbb.opcodeStart;
                irbb.ir.Capacity = hlbb.opcodes.Length;

                if (hlbb.function.Debug == null ||
                    !gdata2.Config.GenerateBytecodeMapping)
                {
                    foreach (var v in hlbb.opcodes.Span)
                    {
                        ParseOpCode(v, optIdx++, irbb);
                    }
                }
                else
                {
                    foreach (var v in hlbb.opcodes.Span)
                    {
                        irbb.AddIR(new IR_DebugSequence(hlbb.function.Debug[optIdx]));
                        ParseOpCode(v, optIdx++, irbb);
                    }
                }
                return irbb;
            }
        }
        private IR_LoadLocalReg CreateLoadLocalReg( int index, bool isValue = false )
        {
            var an = currentAssign;
            if (isValue)
            {
                currentAssign = null;
            }
            else
            {
                an = null;
            }

            return new IR_LoadLocalReg(gdata.Registers[index], an);
        }
        public TypeReference GetLocalRegType( int index )
        {
            return gdata.Registers[index]!.RegisterType;
        }
        private PropertyDefinition GetFieldById( TypeReference tr, int index )
        {
            return container.GetData<IGetField>(tr).GetField(index) ?? throw new InvalidOperationException();
        }
        private IRBasicBlockData GetJmpTarget( IRBasicBlockData irbb, int opIndex, int target )
        {
            var t = opIndex + 1 + target;
            return irbb.transitions.First(x => x.Target.startInHlbc == t).Target;
        }
        private MethodReference GetMethodProto( TypeDefinition type, int pindex )
        {
            return container.GetData<IGetProto>(type).GetProto(pindex) ?? throw new InvalidOperationException();
        }
        private object? GetGlobalData( int idx )
        {
            if (idx == 0)
            {
                return null;
            }
            var t = gdata2.Code.Globals[idx].Value;
            if (t.Kind == HlTypeKind.Obj && ((HlTypeWithObj)t).Obj.Name == "String")
            {
                var str = gdata2.Code.GetUString(gdata2.Code.Constants[gdata2.Code.ConstantIndexes[idx]].Fields[0]);
                return str;
            }
            if (t is HlTypeWithAbsName absName)
            {
                return absName.AbstractName;
            }
            return container.TryGetData<IGlobalValue>(t, out var result) ? result : null;
        }
        private void ParseOpCode(
            HlOpcode code,
            int index,
            IRBasicBlockData irbb )
        {
            var hlc = gdata2.Code;
            var c = code.Kind;
            var op = HlOpCodes.OpCodes[(int)code.Kind];
            currentAssign = null;
            if (gdata.Assigns.TryGetValue(index + 1, out var assign) &&
                assign.Count > 0)
            {
                currentAssign = string.Join('_', assign);
            }
            if (op.Payloads.Length > 0 &&
                op.Payloads[0].HasFlag(HlOpCode.PayloadKind.StoreResult))
            {
                int dstReg = -1;

                IRBase? src = null;

                if (c >= HlOpcodeKind.Mov && c < HlOpcodeKind.Null)
                {
                    var srcId = code.Parameters[1];
                    src = c switch
                    {
                        HlOpcodeKind.Mov => CreateLoadLocalReg(srcId),
                        HlOpcodeKind.Int => new IR_LoadConst(hlc.Ints[srcId]),
                        HlOpcodeKind.Float => new IR_LoadConst(hlc.Floats[srcId]),
                        HlOpcodeKind.Bool => new IR_LoadConst(srcId),
                        HlOpcodeKind.Bytes => new IR_LoadConst(null), //TODO: Fix ME
                        HlOpcodeKind.String => new IR_LoadConst(hlc.GetUString(srcId)),
                        _ => throw new NotSupportedException()
                    };

                }
                else if (c == HlOpcodeKind.Null)
                {
                    src = new IR_LoadConst(null);
                }
                else if (c >= HlOpcodeKind.Add && c <= HlOpcodeKind.Xor)
                {
                    //Opt 3
                    src = new IR_Opt2(
                        CreateLoadLocalReg(code.Parameters[1]),
                        CreateLoadLocalReg(code.Parameters[2]),
                        (IR_Opt2.OptKind)((int)c - HlOpcodeKind.Add)
                        );
                }
                else if (c >= HlOpcodeKind.Neg && c <= HlOpcodeKind.Decr)
                {
                    //Opt 2
                    src = new IR_Opt1(
                        CreateLoadLocalReg(
                            code.Parameters[c < HlOpcodeKind.Incr ? 1 : 0]
                            ),
                        (IR_Opt1.OptKind)((int)c - HlOpcodeKind.Neg)
                        );
                }
                else if (c == HlOpcodeKind.Field || c == HlOpcodeKind.GetThis)
                {
                    var objReg = gdata.Registers[c == HlOpcodeKind.GetThis ? 0 : code.Parameters[1]]!;
                    //Get Field
                    src = new IR_GetField(new IR_LoadLocalReg(objReg),
                        GetFieldById(objReg.RegisterType, code.Parameters[^1]));
                }
                else if (c == HlOpcodeKind.GetGlobal)
                {
                    var gd = GetGlobalData(code.Parameters[1]);
                    if (gd is string)
                    {
                        src = new IR_LoadConst(gd);
                    }
                    else if (gd is IGlobalValue gv)
                    {
                        src = new IR_GetGlobal(gv);
                    }
                    else
                    {
                        src = new IR_LoadConst(null);
                    }
                }
                else if (c == HlOpcodeKind.CallClosure)
                {
                    var args = new IRResult[code.Parameters.Length - 3];
                    for (int i = 0; i < args.Length; i++)
                    {
                        args[i] = CreateLoadLocalReg(code.Parameters[i + 3]);
                    }
                    var srcReg = gdata.Registers[code.Parameters[1]];
                    src = new IR_CallClosure(
                        container.TryGetData<IInvokable>(srcReg!.RegisterType, out var fti) ? fti.Invoke : null,
                        new IR_LoadLocalReg(srcReg),
                        args
                        );
                }
                else if (c >= HlOpcodeKind.Call0 && c <= HlOpcodeKind.CallN)
                {
                    var fidx = hlc.FunctionIndexes[code.Parameters[1]];
                    int argSkip = c == HlOpcodeKind.CallN ? 3 : 2;
                    var args = new IRResult[code.Parameters.Length - argSkip];
                    for (int i = 0; i < args.Length; i++)
                    {
                        args[i] = CreateLoadLocalReg(code.Parameters[i + argSkip]);
                    }

                    if (fidx >= hlc.Functions.Count)
                    {
                        //Native
                        var native = hlc.Natives[fidx - hlc.Functions.Count];

                        src = new IR_CallClosure(
                            container.GetData<IInvokable>(native.Type.Value).Invoke,
                            new IR_GetNative(native),
                            args
                            );
                    }
                    else
                    {
                        var mr = container.GetData<MethodReference>(hlc.Functions[fidx]);
                        if (mr.Name == "__inst_construct__" &&
                            (gdata.Definition.Name != "__inst_construct__" ||
                            code.Parameters[argSkip] != 0))
                        {
                            src = new IR_CallCtor(GetLocalRegType(code.Parameters[argSkip]), args[1..]);
                            dstReg = code.Parameters[argSkip];
                        }
                        else
                        {
                            src = new IR_Call(mr, false, args);
                        }
                    }

                }
                else if (c == HlOpcodeKind.CallThis ||
                    c == HlOpcodeKind.CallMethod)
                {
                    var thisReg = gdata.Registers[c == HlOpcodeKind.CallThis ? 0 : code.Parameters[3]]!;
                    var args = c == HlOpcodeKind.CallThis ?
                        new IRResult[code.Parameters[2] + 1] :
                        new IRResult[code.Parameters[2]]
                        ;
                    args[0] = new IR_LoadLocalReg(thisReg);
                    if (c == HlOpcodeKind.CallThis)
                    {
                        for (int i = 1; i < args.Length; i++)
                        {
                            args[i] = CreateLoadLocalReg(code.Parameters[i + 3 - 1]);
                        }
                    }
                    else
                    {
                        for (int i = 1; i < args.Length; i++)
                        {
                            args[i] = CreateLoadLocalReg(code.Parameters[i + 3]);
                        }
                    }
                    var pindex = code.Parameters[1];
                    var rt = thisReg.RegisterType;
                    if (container.TryGetData<ObjClassData>(rt, out var od))
                    {
                        src = new IR_Call(
                            GetMethodProto(od.TypeDef, pindex),
                            true,
                            args);
                    }
                    else
                    {
                        //Invoke Closure
                        var f = container.GetData<VirtualClassData>(rt);
                        var tm = f.GetField(pindex)!;
                        var iv = container.GetData<IInvokable>(tm.PropertyType);
                        src = new IR_CallClosure(
                            iv.Invoke,
                                new IR_GetField(args[0], tm),
                                 args[1..]
                            );
                    }
                }
                else if (c == HlOpcodeKind.Type)
                {
                    src = new IR_GetType(
                        container.GetTypeRef(hlc.Types[code.Parameters[1]])
                        );
                }
                else if (c == HlOpcodeKind.GetType)
                {
                    src = new IR_GetType(
                            container.GetTypeRef(hlc.Types[code.Parameters[1]])
                            );
                }
                else if (c == HlOpcodeKind.GetTID)
                {
                    src = new IR_GetObjType(
                            CreateLoadLocalReg(code.Parameters[1])
                            );
                }
                else if (c == HlOpcodeKind.New)
                {
                    currentAssign = null;
                    src = null;// new IR_New(GetLocalRegType(code.Parameters[0]));
                }
                else if (c == HlOpcodeKind.ArraySize)
                {
                    src = new IR_GetArraySize(CreateLoadLocalReg(code.Parameters[1]));
                }
                else if (c == HlOpcodeKind.GetArray)
                {
                    src = new IR_GetArrayItem(
                        CreateLoadLocalReg(code.Parameters[1]),
                        CreateLoadLocalReg(code.Parameters[2]),
                        gdata.Registers[code.Parameters[0]]!.RegisterType
                        );
                }
                else if (c == HlOpcodeKind.RefData)
                {
                    src = new IR_RefArray(
                        CreateLoadLocalReg(code.Parameters[1]),
                        new IR_LoadConst(0),
                        gdata.Registers[code.Parameters[0]]!.RegisterType
                        );
                }
                else if (c == HlOpcodeKind.RefOffset)
                {
                    src = new IR_RefOffset(
                        CreateLoadLocalReg(code.Parameters[1]),
                        CreateLoadLocalReg(code.Parameters[2]));
                }
                else if (c == HlOpcodeKind.Ref)
                {
                    src = new IR_Ref(
                        gdata.Registers[code.Parameters[1]]!
                        );
                }
                else if (c == HlOpcodeKind.Unref)
                {
                    src = new IR_Unref(
                        CreateLoadLocalReg(code.Parameters[1]),
                        GetLocalRegType(code.Parameters[0])
                        );
                }
                else if (c == HlOpcodeKind.DynGet)
                {
                    src = new IR_GetDynObj(
                        CreateLoadLocalReg(code.Parameters[1]),
                        hlc.GetUString(code.Parameters[2]));
                }
                else if (c == HlOpcodeKind.SafeCast ||
                        c == HlOpcodeKind.ToDyn ||
                        c == HlOpcodeKind.ToInt ||
                        c == HlOpcodeKind.ToSFloat ||
                        c == HlOpcodeKind.ToUFloat ||
                        c == HlOpcodeKind.ToVirtual)
                {
                    src = new IR_Cast(
                        CreateLoadLocalReg(code.Parameters[1]),
                        gdata.Registers[code.Parameters[0]]!.RegisterType
                        );
                }
                else if (c == HlOpcodeKind.UnsafeCast)
                {
                    src = CreateLoadLocalReg(code.Parameters[1]);
                }
                else if (c == HlOpcodeKind.InstanceClosure)
                {
                    src = new IR_CreateClosure(
                        container.GetData<MethodReference>(hlc.GetFunctionById(code.Parameters[1])!),
                        GetLocalRegType(code.Parameters[0]),
                        false,
                        CreateLoadLocalReg(code.Parameters[2])
                        );
                }
                else if (c == HlOpcodeKind.VirtualClosure)
                {
                    src = new IR_CreateClosure(
                        GetMethodProto((TypeDefinition)GetLocalRegType(code.Parameters[1]), code.Parameters[2]),
                        GetLocalRegType(code.Parameters[0]),
                        true,
                        CreateLoadLocalReg(code.Parameters[1])
                        );
                }
                else if (c == HlOpcodeKind.StaticClosure)
                {
                    var f = hlc.GetFunctionById(code.Parameters[1]);
                    if (f != null)
                    {
                        src = new IR_CreateClosure(
                            container.GetData<MethodReference>(f),
                            GetLocalRegType(code.Parameters[0]),
                            false,
                            new IR_LoadConst(null)
                            );
                    }
                    else
                    {
                        var native = hlc.Natives[hlc.FunctionIndexes[code.Parameters[1]] - hlc.Functions.Count];
                        src = new IR_GetNative(native);
                    }
                }
                else if (c == HlOpcodeKind.MakeEnum)
                {
                    var ctor = container.GetData<EnumClassData>(
                        gdata.Registers[code.Parameters[0]]!.RegisterType
                        ).ItemCtors[code.Parameters[1]];
                    var args = new IRResult[code.Parameters.Length - 3];
                    for (int i = 0; i < args.Length; i++)
                    {
                        args[i] = CreateLoadLocalReg(code.Parameters[i + 3]);
                    }

                    src = new IR_MakeEnum(ctor,
                        args);
                }
                else if (c == HlOpcodeKind.EnumAlloc)
                {
                    var ctor = container.GetData<EnumClassData>(
                        GetLocalRegType(code.Parameters[0])
                            ).ItemTypes[code.Parameters[1]];
                    src = new IR_New(ctor);
                }
                else if (c == HlOpcodeKind.EnumIndex)
                {
                    src = new IR_EnumIndex(CreateLoadLocalReg(code.Parameters[1]));
                }
                else if (c == HlOpcodeKind.EnumField)
                {
                    src = new IR_GetField(
                        CreateLoadLocalReg(code.Parameters[1]),
                        container.GetData<EnumClassData>(
                            gdata.Registers[code.Parameters[1]]!.RegisterType
                            ).ItemTypes[code.Parameters[2]]
                            .Properties[code.Parameters[3]]
                            );
                }
                else if (c == HlOpcodeKind.GetMem ||
                    c == HlOpcodeKind.GetI8 ||
                    c == HlOpcodeKind.GetI16)
                {
                    src = new IR_GetMem(
                        CreateLoadLocalReg(code.Parameters[1]),
                        CreateLoadLocalReg(code.Parameters[2]),
                        c switch
                        {
                            HlOpcodeKind.GetI8 => typeSystem.Byte,
                            HlOpcodeKind.GetI16 => typeSystem.Int16,
                            _ => GetLocalRegType(code.Parameters[0])
                        }
                        );
                }

                if (src != null)
                {
                    if (dstReg == -1)
                    {
                        dstReg = code.Parameters[0];
                    }
                    
                    irbb.AddIR(new IR_SetLocalReg(
                        gdata.Registers[dstReg],
                        src,
                        currentAssign
                        ));
                    currentAssign = null;
                }
            }
            else if (c == HlOpcodeKind.SetArray)
            {
                irbb.AddIR(new IR_SetArrayItem(
                    CreateLoadLocalReg(code.Parameters[0]),
                    CreateLoadLocalReg(code.Parameters[1]),
                    CreateLoadLocalReg(code.Parameters[2])
                    ));
            }
            else if (c == HlOpcodeKind.SetI8 ||
                c == HlOpcodeKind.SetI16 ||
                c == HlOpcodeKind.SetMem)
            {
                irbb.AddIR(new IR_SetMem(
                    CreateLoadLocalReg(code.Parameters[0]),
                    CreateLoadLocalReg(code.Parameters[1]),
                    CreateLoadLocalReg(code.Parameters[2])
                    ));
            }
            else if (c == HlOpcodeKind.Setref)
            {
                irbb.AddIR(new IR_SetRef(
                    CreateLoadLocalReg(code.Parameters[0]),
                    CreateLoadLocalReg(code.Parameters[1])
                    ));
            }
            else if (c == HlOpcodeKind.DynSet)
            {
                irbb.AddIR(new IR_SetDynObj(
                    CreateLoadLocalReg(code.Parameters[0]),
                    CreateLoadLocalReg(code.Parameters[2]),
                    hlc.GetUString(code.Parameters[1])
                    ));
            }
            else if (c == HlOpcodeKind.SetEnumField)
            {
                irbb.AddIR(new IR_SetField(
                    CreateLoadLocalReg(code.Parameters[0]),
                        container.GetData<EnumClassData>(
                            GetLocalRegType(code.Parameters[0])
                            ).ItemTypes[0]
                            .Properties[code.Parameters[1]],
                    CreateLoadLocalReg(code.Parameters[2])
                            )
                    );
            }
            else if (c == HlOpcodeKind.SetField)
            {
                irbb.AddIR(new IR_SetField(
                    CreateLoadLocalReg(code.Parameters[0]),
                    GetFieldById(
                        GetLocalRegType(code.Parameters[0]),
                        code.Parameters[1]),
                    CreateLoadLocalReg(code.Parameters[2], true)
                    ));
            }
            else if (c == HlOpcodeKind.SetThis)
            {
                irbb.AddIR(new IR_SetField(
                    CreateLoadLocalReg(0),
                    GetFieldById(
                        GetLocalRegType(0),
                        code.Parameters[0]),
                    CreateLoadLocalReg(code.Parameters[1])
                    ));
            }
            else if (c == HlOpcodeKind.Ret)
            {
                var r = gdata.Registers[code.Parameters[0]];
                irbb.AddIR(new IR_Ret(r == null ? null : new IR_LoadLocalReg(r)));
            }
            else if (c == HlOpcodeKind.Throw || c == HlOpcodeKind.Rethrow)
            {
                irbb.AddIR(new IR_Throw(
                    c == HlOpcodeKind.Throw ? CreateLoadLocalReg(code.Parameters[0]) : null
                    ));
            }
            else if (c == HlOpcodeKind.JAlways)
            {
                irbb.AddIR(new IR_Jmp(
                    GetJmpTarget(irbb, index, code.Parameters[0])
                    ));
            }
            else if (c >= HlOpcodeKind.JTrue && c <= HlOpcodeKind.JNotNull)
            {
                irbb.AddIR(new IR_JmpConditional1(
                    GetJmpTarget(irbb, index, code.Parameters[1]),
                     c switch
                     {
                         HlOpcodeKind.JTrue => IR_JmpConditional1.ConditionKind.True,
                         HlOpcodeKind.JFalse => IR_JmpConditional1.ConditionKind.False,
                         HlOpcodeKind.JNull => IR_JmpConditional1.ConditionKind.Null,
                         HlOpcodeKind.JNotNull => IR_JmpConditional1.ConditionKind.NotNull,
                         _ => throw new InvalidOperationException()
                     },
                    CreateLoadLocalReg(code.Parameters[0])
                    ));
            }
            else if (c >= HlOpcodeKind.JSLt && c <= HlOpcodeKind.JNotEq)
            {
                irbb.AddIR(new IR_JmpConditional2(
                    GetJmpTarget(irbb, index, code.Parameters[2]),
                    c switch
                    {
                        HlOpcodeKind.JEq => IR_JmpConditional2.ConditionKind.Eq,
                        HlOpcodeKind.JNotEq => IR_JmpConditional2.ConditionKind.NotEq,
                        HlOpcodeKind.JSGt => IR_JmpConditional2.ConditionKind.Greate,
                        HlOpcodeKind.JSLt => IR_JmpConditional2.ConditionKind.Less,
                        HlOpcodeKind.JSGte => IR_JmpConditional2.ConditionKind.NotLess,
                        HlOpcodeKind.JSLte => IR_JmpConditional2.ConditionKind.NotGreate,
                        HlOpcodeKind.JNotLt => IR_JmpConditional2.ConditionKind.NotLess,
                        HlOpcodeKind.JNotGte => IR_JmpConditional2.ConditionKind.NotGreate,
                        HlOpcodeKind.JUGte => IR_JmpConditional2.ConditionKind.SGreate,
                        HlOpcodeKind.JULt => IR_JmpConditional2.ConditionKind.SLess,
                        _ => throw new InvalidOperationException()
                    },
                    CreateLoadLocalReg(code.Parameters[0]),
                    CreateLoadLocalReg(code.Parameters[1])
                    ));
            }
            else if (c == HlOpcodeKind.Switch)
            {
                var t = new IRBasicBlockData[code.Parameters[1]];
                for (int j = 0; j < t.Length; j++)
                {
                    t[j] = GetJmpTarget(irbb, index, code.Parameters[3 + j]);
                }
                irbb.AddIR(new IR_Switch(
                    CreateLoadLocalReg(0), t));
            }
            else if (c == HlOpcodeKind.Assert)
            {
                irbb.AddIR(new IR_Assert("Assert fail!"));
            }
            Debug.Assert(currentAssign == null);
        }
    }
}
