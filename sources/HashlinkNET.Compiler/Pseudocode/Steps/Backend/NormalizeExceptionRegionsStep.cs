using HashlinkNET.Compiler.Pseudocode.Data;
using HashlinkNET.Compiler.Steps;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode.Steps.Backend
{
    /// <summary>
    /// Ensures try-region entry/exit points are stack-neutral. Haxe trap
    /// boundaries can sit in the middle of an expression; CLR protected
    /// regions must start and end with an empty evaluation stack.
    /// </summary>
    internal class NormalizeExceptionRegionsStep : CompileStep
    {
        public override void Execute( IDataContainer container )
        {
            var gdata = container.GetGlobalData<FuncEmitGlobalData>();
            var body = gdata.Definition.Body;
            var instructions = body.Instructions;
            if (instructions.Count == 0 || body.ExceptionHandlers.Count == 0)
            {
                return;
            }

            var heights = new int[instructions.Count];
            var height = 0;
            for (var i = 0; i < instructions.Count; i++)
            {
                heights[i] = height;
                height += StackDelta(instructions[i]);
                if (height < 0)
                {
                    height = 0;
                }

                switch (instructions[i].OpCode.Code)
                {
                    case Code.Br:
                    case Code.Br_S:
                    case Code.Leave:
                    case Code.Leave_S:
                    case Code.Ret:
                    case Code.Throw:
                    case Code.Rethrow:
                    case Code.Switch:
                        height = 0;
                        break;
                }
            }

            int Index( Instruction inst ) => instructions.IndexOf(inst);

            // A catch/filter handler is entered only by the runtime. Any
            // explicit branch into a handler is invalid; drop the regions and
            // redirect those branches past the handler.
            foreach (var eh in body.ExceptionHandlers)
            {
                var hs = eh.HandlerStart == null ? -1 : Index(eh.HandlerStart);
                var he = eh.HandlerEnd == null ? instructions.Count : Index(eh.HandlerEnd);
                if (hs < 0)
                {
                    continue;
                }
                for (var i = 0; i < instructions.Count; i++)
                {
                    var tgt = instructions[i].Operand switch
                    {
                        Instruction one => one,
                        Instruction[] many when many.Length > 0 => many[0],
                        _ => null
                    };
                    if (tgt == null)
                    {
                        continue;
                    }
                    var ti = Index(tgt);
                    if (ti >= hs && ti < he && (i < hs || i >= he))
                    {
                        DropExceptionRegions(body);
                        return;
                    }
                }
            }

            foreach (var eh in body.ExceptionHandlers)
            {
                var tryStart = Index(eh.TryStart);
                if (tryStart > 0 && heights[tryStart] != 0)
                {
                    for (var i = tryStart - 1; i >= 0; i--)
                    {
                        if (heights[i] == 0)
                        {
                            eh.TryStart = instructions[i];
                            break;
                        }
                    }
                }

                var tryEnd = eh.TryEnd == null ? instructions.Count : Index(eh.TryEnd);
                if (tryEnd < instructions.Count && tryEnd > 0 && heights[tryEnd] != 0)
                {
                    for (var i = tryEnd + 1; i < instructions.Count; i++)
                    {
                        if (heights[i] == 0)
                        {
                            eh.TryEnd = instructions[i];
                            break;
                        }
                    }
                }

                // Branches from outside the try region into it are invalid.
                // Expand the region to include their source instruction.
                tryStart = Index(eh.TryStart);
                tryEnd = eh.TryEnd == null ? instructions.Count : Index(eh.TryEnd);
                var handlerStart = eh.HandlerStart == null ? -1 : Index(eh.HandlerStart);
                for (var pass = 0; pass < 8; pass++)
                {
                    var changed = false;
                    for (var i = 0; i < instructions.Count; i++)
                    {
                        var ins = instructions[i];
                        if (ins.Operand is Instruction tgt0 &&
                            Index(tgt0) >= tryStart && Index(tgt0) < tryEnd &&
                            (i >= tryEnd || (handlerStart >= 0 && i >= handlerStart && i < (eh.HandlerEnd == null ? instructions.Count : Index(eh.HandlerEnd)))))
                        {
                            // A branch from after the handler back into the try
                            // cannot be represented in a single protected
                            // region. Drop the region; the remaining body is
                            // still valid straight-line IL.
                            DropExceptionRegions(body);
                            return;
                        }

                        if (instructions[i].Operand is not Instruction tgt)
                        {
                            continue;
                        }
                        var targetIndex = Index(tgt);
                        if (targetIndex < tryStart || targetIndex >= tryEnd)
                        {
                            continue;
                        }
                        if (i >= tryStart && i < tryEnd)
                        {
                            continue;
                        }
                        if (i < tryStart)
                        {
                            var newStart = i;
                            while (newStart > 0 && heights[newStart] != 0)
                            {
                                newStart--;
                            }
                            eh.TryStart = instructions[newStart];
                            tryStart = newStart;
                            changed = true;
                        }
                        else if (i >= tryEnd && (handlerStart < 0 || i < handlerStart))
                        {
                            var newEnd = i + 1;
                            while (newEnd < instructions.Count && heights[newEnd] != 0)
                            {
                                newEnd++;
                            }
                            eh.TryEnd = newEnd < instructions.Count
                                ? instructions[newEnd]
                                : null;
                            tryEnd = newEnd;
                            changed = true;
                        }
                    }
                    if (!changed)
                    {
                        break;
                    }
                }
            }
        }

        private static void DropExceptionRegions( MethodBody body )
        {
            var instructions = body.Instructions;
            foreach (var eh in body.ExceptionHandlers)
            {
                if (eh.HandlerStart == null)
                {
                    continue;
                }
                var hs = instructions.IndexOf(eh.HandlerStart);
                var he = eh.HandlerEnd == null
                    ? instructions.Count
                    : instructions.IndexOf(eh.HandlerEnd);
                var end = eh.HandlerEnd;
                foreach (var ins in instructions)
                {
                    if (ins.Operand is Instruction one)
                    {
                        var ti = instructions.IndexOf(one);
                        if (ti >= hs && ti < he)
                        {
                            ins.Operand = end;
                        }
                    }
                    else if (ins.Operand is Instruction[] many)
                    {
                        for (var i = 0; i < many.Length; i++)
                        {
                            var ti = instructions.IndexOf(many[i]);
                            if (ti >= hs && ti < he)
                            {
                                many[i] = end!;
                            }
                        }
                    }
                }
            }

            body.ExceptionHandlers.Clear();
            foreach (var ins in instructions)
            {
                if (ins.OpCode.Code is Code.Leave or Code.Leave_S)
                {
                    ins.OpCode = OpCodes.Br;
                }
            }
        }
        private static int StackDelta( Instruction ins )
        {
            var code = ins.OpCode.Code;
            if (ins.Operand is MethodReference mr)
            {
                if (code == Code.Newobj)
                {
                    return 1 - mr.Parameters.Count;
                }
                if (code is Code.Call or Code.Callvirt)
                {
                    return (mr.ReturnType.MetadataType == MetadataType.Void ? 0 : 1) -
                        (mr.Parameters.Count + (mr.HasThis ? 1 : 0));
                }
            }
            if (ins.Operand is CallSite cs && code == Code.Calli)
            {
                return (cs.ReturnType.MetadataType == MetadataType.Void ? 0 : 1) -
                    (cs.Parameters.Count + (cs.HasThis ? 1 : 0) + 1);
            }

            return code switch
            {
                Code.Ldarg or Code.Ldarg_0 or Code.Ldarg_1 or Code.Ldarg_2 or Code.Ldarg_3 or
                Code.Ldarg_S or Code.Ldarga or Code.Ldarga_S or
                Code.Ldloc or Code.Ldloc_0 or Code.Ldloc_1 or Code.Ldloc_2 or Code.Ldloc_3 or
                Code.Ldloc_S or Code.Ldloca or Code.Ldloca_S or
                Code.Ldc_I4 or Code.Ldc_I4_0 or Code.Ldc_I4_1 or Code.Ldc_I4_2 or Code.Ldc_I4_3 or
                Code.Ldc_I4_4 or Code.Ldc_I4_5 or Code.Ldc_I4_6 or Code.Ldc_I4_7 or Code.Ldc_I4_8 or
                Code.Ldc_I4_M1 or Code.Ldc_I4_S or Code.Ldc_I8 or Code.Ldc_R4 or Code.Ldc_R8 or
                Code.Ldnull or Code.Ldstr or Code.Ldsfld or Code.Ldsflda or Code.Ldftn or
                Code.Ldvirtftn or Code.Dup => 1,
                Code.Starg or Code.Starg_S or Code.Stloc or Code.Stloc_0 or Code.Stloc_1 or
                Code.Stloc_2 or Code.Stloc_3 or Code.Stloc_S or Code.Stsfld or Code.Pop => -1,
                Code.Stfld => -2,
                Code.Ldfld or Code.Ldflda => 0,
                Code.Newarr or Code.Ldelem_Any or Code.Ldelema => -1,
                Code.Ldelem_I1 or Code.Ldelem_U1 or Code.Ldelem_I2 or Code.Ldelem_U2 or
                Code.Ldelem_I4 or Code.Ldelem_U4 or Code.Ldelem_I8 or Code.Ldelem_I or
                Code.Ldelem_R4 or Code.Ldelem_R8 or Code.Ldelem_Ref => -1,
                Code.Stelem_Any or Code.Stelem_I or Code.Stelem_I1 or Code.Stelem_I2 or
                Code.Stelem_I4 or Code.Stelem_I8 or Code.Stelem_R4 or Code.Stelem_R8 or
                Code.Stelem_Ref => -3,
                Code.Add or Code.Sub or Code.Mul or Code.Div or Code.Div_Un or Code.Rem or
                Code.Rem_Un or Code.And or Code.Or or Code.Xor or Code.Shl or Code.Shr or
                Code.Shr_Un or Code.Ceq or Code.Cgt or Code.Cgt_Un or Code.Clt or Code.Clt_Un => -1,
                Code.Neg or Code.Not or Code.Conv_I1 or Code.Conv_I2 or Code.Conv_I4 or
                Code.Conv_I8 or Code.Conv_R4 or Code.Conv_R8 or Code.Conv_U1 or Code.Conv_U2 or
                Code.Conv_U4 or Code.Conv_U8 or Code.Conv_I or Code.Conv_U or Code.Conv_R_Un or
                Code.Box or Code.Unbox or Code.Unbox_Any or Code.Castclass or Code.Isinst => 0,
                Code.Ldobj => 0,
                Code.Stobj or Code.Initobj => -1,
                Code.Brtrue or Code.Brtrue_S or Code.Brfalse or Code.Brfalse_S => -1,
                Code.Beq or Code.Beq_S or Code.Bne_Un or Code.Bne_Un_S or Code.Bgt or Code.Bgt_S or
                Code.Bgt_Un or Code.Bgt_Un_S or Code.Bge or Code.Bge_S or Code.Bge_Un or
                Code.Bge_Un_S or Code.Blt or Code.Blt_S or Code.Blt_Un or Code.Blt_Un_S or
                Code.Ble or Code.Ble_S or Code.Ble_Un or Code.Ble_Un_S => -2,
                _ => 0
            };
        }
    }
}
