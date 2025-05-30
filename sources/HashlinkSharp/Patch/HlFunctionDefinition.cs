using Hashlink.Patch.Reader;
using Hashlink.Reflection.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PK = Hashlink.Patch.HlOpCode.PayloadKind;
using OP = HashlinkNET.Bytecode.HlOpcodeKind;
using Hashlink.Marshaling;
using System.Reflection.Emit;
using Hashlink.Reflection.Members;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Hashlink.Reflection.Members.Object;
using Hashlink.Reflection;
using Hashlink.UnsafeUtilities;
using Hashlink.Patch.Writer;
using Hashlink.Proxy.Objects;
using System.Collections.Concurrent;

namespace Hashlink.Patch
{
    public unsafe class HlFunctionDefinition
    {
        private static readonly ConcurrentBag<IHashlinkPointer> funcConstants = [];
        public List<HlFunctionReg> Parameters
        {
            get; set;
        } = [];
        public HashlinkType Return
        {
            get; set;
        } = HashlinkMarshal.Module.KnownTypes.Void;
        public List<HlFunctionReg> LocalRegisters
        {
            get; set;
        } = [];
        public List<HlInstruction> Instructions
        {
            get; set;
        } = [];
        public HashlinkObjectType? DeclaringType
        {
            get; set;
        }
        public HlFunctionReg GetRegisterByIndex( int index )
        {
            if (index < Parameters.Count)
            {
                return Parameters[index];
            }
            return LocalRegisters[index - Parameters.Count];
        }

        public void ReadInstructionsFrom( HlOpCodeReader reader )
        {
            Instructions.Clear();
            List<int> buffer = new(8);
            List<object?> operands = new(8);
            List<(HlInstruction ins, int pId, int offset)> jumpFix = [];
            while (!reader.IsEmpty)
            {
                var op = HlOpCodes.OpCodes[reader.Read(PK.None)];
                var epCount = 0;
                var tProvider = -1;
                int fixReflect = -1;
                int fixReflectOp = -1;

                var instruction = new HlInstruction()
                {
                    OpCode = op,
                    Index = Instructions.Count
                };

                

                for (int i = 0; i < op.Payloads.Length; i++)
                {
                    var pk = op.Payloads[i];
                    buffer.Add(reader.Read(
                        pk
                        ));
                    
                    if (pk.HasFlag(PK.VariableCount))
                    {
                        epCount = buffer[i] - op.Payloads.Length + 3;
                    }
                }

                for (int i = 0; i < epCount; i++)
                {
                    buffer.Add(reader.Read(op.VariablePayload!.Value));
                }

                for (int i = 0; i < buffer.Count; i++)
                {
                    var k = i < op.Payloads.Length ? op.Payloads[i] : op.VariablePayload!.Value;
                    var val = buffer[i];
                    if (k.HasFlag(PK.VariableCount))
                    {
                        operands.Add(val);
                        continue;
                    }
                    if (k.HasFlag(PK.TypeProvider))
                    {
                        tProvider = operands.Count;
                    }
                    if (k.HasFlag(PK.RequestTypeInfo))
                    {
                        if (!k.HasFlag(PK.DeclaringOnThis))
                        {
                            fixReflect = i;
                            fixReflectOp = operands.Count;
                            operands.Add(null);
                            continue;
                        }
                        if (DeclaringType == null)
                        {
                            throw new InvalidOperationException();
                        }
                        if (k.HasFlag(PK.Field))
                        {
                            operands.Add(DeclaringType.FindFieldById(val));
                            continue;
                        }
                        else if (k.HasFlag(PK.Proto))
                        {
                            operands.Add(DeclaringType.FindProtoById(val));
                            continue;
                        }
                    }
                    else if (k.HasFlag(PK.Offset))
                    {
                        jumpFix.Add((instruction, operands.Count, Instructions.Count + 1 + val));
                        operands.Add(null);
                        continue;
                    }
                    else if (k.HasFlag(PK.Register))
                    {
                        operands.Add(GetRegisterByIndex(val));
                        continue;
                    }
                    else if (k.HasFlag(PK.Type))
                    {
                        operands.Add(HashlinkMarshal.Module.Types[val]);
                        continue;
                    }
                    else if (k.HasFlag(PK.Function))
                    {
                        operands.Add(HashlinkMarshal.Module.GetFunctionByFIndex(val));
                        continue;
                    }
                    else if (k.HasFlag(PK.Impl))
                    {
                        operands.Add(val);
                        continue;
                    }
                    else if (k.HasFlag(PK.IntIndex))
                    {
                        operands.Add(HashlinkMarshal.Module.Ints[val]);
                        continue;
                    }
                    else if (k.HasFlag(PK.FloatIndex))
                    {
                        operands.Add(HashlinkMarshal.Module.Floats[val]);
                        continue;
                    }
                    else if (k.HasFlag(PK.StringIndex))
                    {
                        operands.Add(HashlinkMarshal.Module.Strings[val]);
                        continue;
                    }
                    else if (k.HasFlag(PK.GlobalIndex))
                    {
                        var gv = HashlinkMarshal.Module.Globals[val];
                        operands.Add(gv);
                        continue;
                    }
                    else if (k.HasFlag(PK.BytesIndex))
                    {
                        operands.Add(
                            (nint)HashlinkMarshal.Module.NativeModule->code->bytes +
                            HashlinkMarshal.Module.NativeModule->code->bytes_pos[val]);
                        continue;
                    }
                    operands.Add(null);
                }

                //Fix Reflect
                if (fixReflect != -1)
                {
                    if (tProvider == -1)
                    {
                        throw new InvalidOperationException();
                    }
                    if (operands[tProvider] is not HashlinkType t)
                    {
                        t = ((HlFunctionReg)operands[tProvider]!).Type;
                    }
                    if (t is HashlinkObjectType objType)
                    {
                        if (op.Payloads[fixReflect].HasFlag(PK.Proto))
                        {
                            operands[fixReflectOp] = objType.FindProtoById(buffer[fixReflect]);
                        }
                        else
                        {
                            operands[fixReflectOp] = objType.FindFieldById(buffer[fixReflect]);
                        }
                    }
                    else if (t is HashlinkVirtualType virtType)
                    {
                        operands[fixReflectOp] = virtType.Fields[buffer[fixReflect]];
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }

                if (op == HlOpCodes.OEnumField)
                {
                    operands[3] = ((HashlinkEnumType)((HlFunctionReg)operands[1]!).Type).Constructs[buffer[2]];
                }
                else if (op == HlOpCodes.OSetEnumField)
                {
                    operands[2] = ((HashlinkEnumType)((HlFunctionReg)operands[1]!).Type).Constructs[0];
                }

                instruction.Operands = operands.ToArray()!;

                Instructions.Add(instruction);

                operands.Clear();
                buffer.Clear();
                reader.MoveNext();
            }

            FixRegisterId();

            //Fix jump 
            foreach ((var ins, int pId, int offset) in jumpFix)
            {
                ins.Operands[pId] = Instructions[offset];
            }
        }
        private void FixInstructionId()
        {
            for (int i = 0; i < Instructions.Count; i++)
            {
                Instructions[i].Index = i;
            }
        }
        private void FixRegisterId()
        {
            for (int i = 0; i < Parameters.Count; i++)
            {
                Parameters[i].Index = i;
            }
            for (int i = 0; i < LocalRegisters.Count; i++)
            {
                LocalRegisters[i].Index = i + Parameters.Count;
            }
        }
        public void VerifyOpCodes()
        {
            FixInstructionId();
            FixRegisterId();
            for (int i = 0; i < Instructions.Count; i++)
            {
                var ins = Instructions[i];
                var op = ins.OpCode;
                if (ins.Operands.Length != op.Payloads.Length)
                {
                    if (ins.Operands.Length < op.Payloads.Length || op.VariablePayload == null)
                    {

                        throw new InvalidProgramException($"Number of operands does not match in {ins}");
                    }
                }
                if (op.OpCode >= OP.Call0 && op.OpCode <= OP.Call4)
                {
                    if (ins.Operands.Length != (op.OpCode - OP.Call0 + 2))
                    {
                        throw new InvalidProgramException($"Number of operands does not match in {ins}");
                    }
                }
                if (op.OpCode == OP.CallN)
                {
                    var f = ins.Operands[1] as IHashlinkFunc ?? throw new InvalidProgramException($"The operand type does not match, it should be IHashlinkFunc in {ins}");
                    if (ins.Operands.Length != (f.FuncType.ArgTypes.Length + 3))
                    {
                        throw new InvalidProgramException($"Number of operands does not match in {ins}");
                    }
                }
                if (op.OpCode == OP.CallMethod || op.OpCode == OP.CallThis)
                {
                    HashlinkFuncType ft;
                    if (ins.Operands[1] is HashlinkObjectProto p)
                    {
                        ft = p.Function.FuncType;
                    }
                    else if (ins.Operands[1] is HashlinkObjectField f && f.FieldType is HashlinkFuncType fft)
                    {
                        ft = fft;
                    }
                    else
                    {
                        throw new InvalidProgramException($"The operand type does not match, it should be HashlinkObjectProto or HashlinkObjectField in {ins}");
                    }
                    if (ins.Operands.Length != (ft.ArgTypes.Length + 3 + OP.CallMethod - op.OpCode))
                    {
                        throw new InvalidProgramException($"Number of operands does not match in {ins}");
                    }
                }
                for (int j = 0; j < ins.Operands.Length; j++)
                {
                    var ope = ins.Operands[j];
                    if (ope == null)
                    {
                        throw new InvalidProgramException();
                    }
                }
            }
        }
        public void ReadFrom( HashlinkFunction from )
        {
            Parameters.Clear();
            LocalRegisters.Clear();

            Return = from.FuncType.ReturnType;
            Parameters.AddRange(
                from.FuncType.ArgTypes.Select(x => new HlFunctionReg(x))
                );
            LocalRegisters.AddRange(
                from.LocalRegisters.Skip(Parameters.Count).Select(x => new HlFunctionReg(x))
                );
            DeclaringType = (HashlinkObjectType?) from.DeclaringType;

            ReadInstructionsFrom(
                from.CreateOpCodeReader()
                );
        }

        private List<CustomHashlinkFunction> customFunctions = [];

        public void WriteOpCodes( HlOpCodeWriter writer, Dictionary<object, int> constantsLookup )
        {
            for (int i = 0; i < Instructions.Count; i++)
            {
                var ins = Instructions[i];
                writer.MoveNext(ins.OpCode.OpCode);

                for (int j = 0; j < ins.Operands.Length; j++)
                {
                    var ope = ins.Operands[j];
                    var k = j < ins.OpCode.Payloads.Length ? ins.OpCode.Payloads[j] : ins.OpCode.VariablePayload!.Value;
                    var val = 0;
                    if (ope is HlFunctionReg reg)
                    {
                        val = reg.Index;
                    }
                    else if (ope is HashlinkObjectField of)
                    {
                        val = of.Index;
                    }
                    else if (ope is HashlinkObjectProto hop)
                    {
                        val = hop.ProtoIndex;
                    }
                    else if (ope is IHashlinkFunc func)
                    {
                        val = func.FunctionIndex;
                    }
                    else if (ope is HashlinkType type)
                    {
                        val = type.TypeIndex;
                    }
                    else if (ope is HlInstruction tins)
                    {
                        val = tins.Index - i - 1;
                    }
                    else if (ope is HashlinkGlobal glob)
                    {
                        val = glob.Index;
                    }
                    else if (k.HasFlag(PK.VariableCount))
                    {
                        val = ins.Operands.Length - 3;
                    }
                    else if (k.HasFlag(PK.Impl))
                    {
                        val = (int)ope;
                    }
                    else if (k.HasFlag(PK.IndexedConstants))
                    {
                        val = constantsLookup[ope];
                    }
                    else if (ins.OpCode == HlOpCodes.OGetGlobal) // Not HashlinkGlobal
                    {
                        val = (int)((uint)constantsLookup[ope] | 0x80000000);
                    }
                    writer.Write(val, k);
                }
            }
        }

        public nint Compile()
        {
            FixRegisterId();
            FixInstructionId();

            //Collect Constants

            HashSet<object> constantsSet = [];
            foreach (var v in Instructions)
            {
                (var pt, var idx) = v.OpCode.Payloads.Select(( x, i ) => (x, i)).FirstOrDefault(x => x.x.HasFlag(PK.IndexedConstants));
                if (pt.HasFlag(PK.GlobalIndex))
                {
                    if (v.Operands[idx] is not HashlinkGlobal)
                    {
                        constantsSet.Add(v.Operands[idx]);
                    }
                }
                if (pt.HasFlag(PK.IndexedConstants))
                {
                    constantsSet.Add(v.Operands[idx]);
                }
            }

            var fptrs = GC.AllocateArray<nint>(
                HashlinkMarshal.Module.Functions.Length + customFunctions.Count, true
                );
            var constantsOwner = GC.AllocateArray<long>(
                constantsSet.Count + 1
                );
            fixed (long* constants = constantsOwner)
            {

                Dictionary<object, int> constantsLookup = [];

                {
                    var cid = 0;
                    foreach (var v in constantsSet)
                    {
                        if (v is int @int)
                        {
                            *(int*)(constants + cid) = @int;
                            constantsLookup.Add(v, cid * 2);
                        }
                        else if (v is float @float)
                        {
                            *(float*)(constants + cid) = @float;
                            constantsLookup.Add(v, cid);
                        }
                        else if (v is nint bytes)
                        {
                            *(nint*)(constants + cid) = bytes;
                            constantsLookup.Add(v, cid);
                        }
                        else if (v is string str)
                        {
                            *(nint*)(constants + cid) = Marshal.StringToHGlobalUni(str);
                            constantsLookup.Add(v, cid);
                        }
                        else if (v is IHashlinkPointer hstr)
                        {
                            *(nint*)(constants + cid) = hstr.HashlinkPointer;
                            funcConstants.Add(hstr);
                            constantsLookup.Add(v, cid);
                        }
                        cid++;
                    }
                }

                HL_code fcode = new()
                {
                    floats = (double*)constants,
                    nfloats = constantsSet.Count,
                    ints = (int*)constants,
                    nints = constantsSet.Count,
                    ustrings = (char**)constants,
                    nstrings = constantsSet.Count,
                    hasdebug = false
                };

                var writer = new HlNativeOpCodeWriter(Instructions.Count);

                WriteOpCodes(writer, constantsLookup);

                new ReadOnlySpan<nint>(
                    HashlinkMarshal.Module.NativeModule->functions_ptrs, HashlinkMarshal.Module.Functions.Length).CopyTo(fptrs);

                Span<nint> regs = stackalloc nint[Parameters.Count + LocalRegisters.Count];
                for (int i = 0; i < Parameters.Count; i++)
                {
                    regs[i] = (nint)Parameters[i].Type.NativePointer;
                }
                for (int i = 0; i < LocalRegisters.Count; i++)
                {
                    regs[i + Parameters.Count] = (nint)LocalRegisters[i].Type.NativePointer;
                }

                fixed (void* pfptrs = fptrs)
                {
                    HL_module fmodule = *HashlinkMarshal.Module.NativeModule with
                    {
                        code = &fcode,
                        functions_ptrs = (void**)pfptrs,
                        jit_debug = null,
                        functions_hashs = null,
                    };
                    HL_type_func tf = new()
                    {
                        nargs = Parameters.Count
                    };
                    HL_type t = new()
                    {
                        kind = TypeKind.HFUN,
                    };
                    t.data.func = &tf;
                    HL_function func = new()
                    {
                        nregs = regs.Length,
                        type = &t,
                        nops = Instructions.Count,
                        ops = (HL_opcode*) Unsafe.AsPointer(ref writer.opcodes[0]),
                        regs = (HL_type**) Unsafe.AsPointer(ref regs.GetPinnableReference())
                    };
                    var ctx = hl_jit_alloc();
                    hl_jit_init(ctx, &fmodule);
                    nint result = hl_jit_function(ctx, &fmodule, &func, constants);
                    var basePtr = (nint)hl_jit_code(ctx, &fmodule, &fmodule.codesize, (void**) &fmodule.jit_debug, null);
                    hl_jit_free(ctx, false);
                    return result + basePtr;
                }
            }
        }
    }
}
