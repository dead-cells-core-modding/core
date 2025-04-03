using Hashlink.Patch.Reader;
using Hashlink.Reflection.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PK = Hashlink.Patch.HlOpCode.PayloadKind;
using OP = Hashlink.HL_opcode.OpCodes;
using Hashlink.Marshaling;
using System.Reflection.Emit;
using Hashlink.Reflection.Members;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Hashlink.Reflection.Members.Object;
using Hashlink.Reflection;
using Hashlink.UnsafeUtilities;

namespace Hashlink.Patch
{
    public unsafe class HlFunctionDefinition
    {
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
                var op = HlOpCodes.OpCodes[reader.Read()];
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
                    if (i >= 3)
                    {
                        break;
                    }
                    buffer.Add(reader.Read());
                    var pk = op.Payloads[i];
                    if (pk.HasFlag(PK.VariableCount))
                    {
                        epCount = buffer[i];
                    }
                }

                for (int i = 0; i < epCount; i++)
                {
                    buffer.Add(reader.Read());
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
                        operands.Add(HashlinkMarshal.Module.Globals[val]);
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
        private void WriteOpCode( HL_opcode* op, int val, 
            ref int idx, 
            MemoryBlock<int> extra)
        {
            if (idx == 0)
            {
                op->p1 = val;
            }
            else if (idx == 1)
            {
                op->p2 = val;
            }
            else if (idx == 2)
            {
                op->p3 = val;
            }
            else
            {
                if (op->extra == null)
                {
                    op->extra = extra.Alloc(4);
                }
                else
                {
                    var os = extra.GetSize(op->extra);
                    if (os <= idx)
                    {
                        op->extra = extra.Expand(op->extra, os << 1);
                    }
                }
                op->extra[idx - 3] = val;
            }
            idx++;
        }
        private (IMemoryOwner<HL_opcode>, MemoryBlock<int>) WriteOpCodes( Dictionary<object, int> constantsLookup )
        {
            var opO = MemoryPool<HL_opcode>.Shared.Rent(Instructions.Count);
            MemoryBlock<int> extra = new();

            fixed (void* opp = opO.Memory.Span)
            {
                HL_opcode* op = (HL_opcode*)opp;
                for (int i = 0; i < Instructions.Count; i++)
                {
                    int idx = 0;
                    var ins = Instructions[i];
                    op->op = ins.OpCode.OpCode;

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
                        WriteOpCode(op, val, ref idx, extra);
                    }
                    op++;
                }
            }

            return (opO, extra);
        }

        public nint Compile()
        {
            FixRegisterId();

            //Collect Constants

            HashSet<object> constantsSet = [];
            foreach (var v in Instructions)
            {
                (var pt, var idx) = v.OpCode.Payloads.Select(( x, i ) => (x, i)).FirstOrDefault(x => x.x.HasFlag(PK.IndexedConstants));
                if (pt.HasFlag(PK.IndexedConstants))
                {
                    constantsSet.Add(v.Operands[idx]);
                }
            }

            using var fptrs = MemoryPool<nint>.Shared.Rent(
                HashlinkMarshal.Module.Functions.Length + customFunctions.Count
                );
            using var constantsOwner = MemoryPool<long>.Shared.Rent(
                constantsSet.Count
                );
            fixed (long* constants = constantsOwner.Memory.Span)
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

                var opcodes = WriteOpCodes(constantsLookup);

                var fptrsS = fptrs.Memory.Span;
                new ReadOnlySpan<nint>(
                    HashlinkMarshal.Module.NativeModule->functions_ptrs, HashlinkMarshal.Module.Functions.Length).CopyTo(fptrsS);

                Span<nint> regs = stackalloc nint[Parameters.Count + LocalRegisters.Count];
                for (int i = 0; i < Parameters.Count; i++)
                {
                    regs[i] = (nint)Parameters[i].Type.NativePointer;
                }
                for (int i = 0; i < LocalRegisters.Count; i++)
                {
                    regs[i + Parameters.Count] = (nint)LocalRegisters[i].Type.NativePointer;
                }

                fixed (void* pfptrs = fptrsS)
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
                        ops = (HL_opcode*) Unsafe.AsPointer(ref opcodes.Item1.Memory.Span.GetPinnableReference()),
                        regs = (HL_type**) Unsafe.AsPointer(ref regs.GetPinnableReference())
                    };
                    var ctx = hl_jit_alloc();
                    hl_jit_init(ctx, &fmodule);
                    nint result = hl_jit_function(ctx, &fmodule, &func, constants);
                    var basePtr = (nint)hl_jit_code(ctx, &fmodule, &fmodule.codesize, (void**) &fmodule.jit_debug, null);
                    hl_jit_free(ctx, false);
                    opcodes.Item2.Dispose();
                    return result + basePtr;
                }
            }
        }
    }
}
