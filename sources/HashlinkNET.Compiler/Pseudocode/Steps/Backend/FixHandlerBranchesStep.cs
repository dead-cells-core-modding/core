using HashlinkNET.Compiler.Pseudocode.Data;
using HashlinkNET.Compiler.Steps;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode.Steps.Backend
{
    /// <summary>
    /// Replaces conditional branches that leave a protected region with a
    /// conditional skip followed by <c>leave</c>. The CLR requires leaving a
    /// try/catch handler through <c>leave</c>; brtrue/brfalse across a region
    /// boundary produces invalid IL.
    /// </summary>
    internal class FixHandlerBranchesStep : CompileStep
    {
        public override void Execute( IDataContainer container )
        {
            var gdata = container.GetGlobalData<FuncEmitGlobalData>();
            var body = gdata.Definition.Body;
            if (body.ExceptionHandlers.Count == 0 || body.Instructions.Count == 0)
            {
                return;
            }

            var instructions = body.Instructions;
            var rewrites = new List<(int Index, OpCode Inverse, Instruction Target, bool LeaveFallthrough, Instruction? Next)>();

            for (var i = 0; i < instructions.Count; i++)
            {
                var ins = instructions[i];
                var inverse = ins.OpCode.Code switch
                {
                    Code.Brtrue or Code.Brtrue_S => OpCodes.Brfalse,
                    Code.Brfalse or Code.Brfalse_S => OpCodes.Brtrue,
                    Code.Beq or Code.Beq_S => OpCodes.Bne_Un,
                    Code.Bne_Un or Code.Bne_Un_S => OpCodes.Beq,
                    Code.Bgt or Code.Bgt_S => OpCodes.Ble,
                    Code.Ble or Code.Ble_S => OpCodes.Bgt,
                    Code.Blt or Code.Blt_S => OpCodes.Bge,
                    Code.Bge or Code.Bge_S => OpCodes.Blt,
                    Code.Bgt_Un or Code.Bgt_Un_S => OpCodes.Ble_Un,
                    Code.Ble_Un or Code.Ble_Un_S => OpCodes.Bgt_Un,
                    Code.Blt_Un or Code.Blt_Un_S => OpCodes.Bge_Un,
                    Code.Bge_Un or Code.Bge_Un_S => OpCodes.Blt_Un,
                    _ => default
                };
                if (inverse.Code == Code.Nop && ins.OpCode.Code != Code.Nop)
                {
                    continue;
                }
                if (ins.Operand is not Instruction target)
                {
                    continue;
                }

                var targetIndex = instructions.IndexOf(target);
                if (targetIndex < 0)
                {
                    continue;
                }

                var needsLeave = false;
                var leaveFallthrough = false;
                Instruction? fallthroughTarget = null;
                foreach (var eh in body.ExceptionHandlers)
                {
                    var tryStart = instructions.IndexOf(eh.TryStart);
                    var tryEnd = eh.TryEnd == null
                        ? instructions.Count
                        : instructions.IndexOf(eh.TryEnd);
                    var handlerStart = eh.HandlerStart == null
                        ? -1
                        : instructions.IndexOf(eh.HandlerStart);
                    var handlerEnd = eh.HandlerEnd == null
                        ? instructions.Count
                        : instructions.IndexOf(eh.HandlerEnd);

                    var inTry = i >= tryStart && i < tryEnd;
                    var inHandler = handlerStart >= 0 && i >= handlerStart && i < handlerEnd;
                    if (!inTry && !inHandler)
                    {
                        continue;
                    }

                    var targetInTry = targetIndex >= tryStart && targetIndex < tryEnd;
                    var targetInHandler = handlerStart >= 0 &&
                        targetIndex >= handlerStart && targetIndex < handlerEnd;
                    if ((inTry && !targetInTry) || (inHandler && !targetInHandler))
                    {
                        needsLeave = true;
                        fallthroughTarget = inHandler ? eh.HandlerEnd : ins.Next;
                        if (fallthroughTarget != null)
                        {
                            var nextIndex = instructions.IndexOf(fallthroughTarget);
                            var nextInTry = nextIndex >= tryStart && nextIndex < tryEnd;
                            var nextInHandler = handlerStart >= 0 &&
                                nextIndex >= handlerStart && nextIndex < handlerEnd;
                            leaveFallthrough = inHandler || (inTry && !nextInTry);
                        }
                        break;
                    }
                }

                if (needsLeave)
                {
                    rewrites.Add((i, inverse, target, leaveFallthrough, fallthroughTarget));
                }
            }

            for (var i = rewrites.Count - 1; i >= 0; i--)
            {
                var (index, inverse, target, leaveFallthrough, next) = rewrites[i];
                var ins = instructions[index];
                var skip = Instruction.Create(OpCodes.Nop);
                var leave = Instruction.Create(OpCodes.Leave, target);
                ins.OpCode = inverse;
                ins.Operand = skip;
                instructions.Insert(index + 1, leave);
                instructions.Insert(index + 2, skip);
                if (leaveFallthrough && next != null)
                {
                    instructions.Insert(index + 3, Instruction.Create(OpCodes.Leave, next));
                }
            }

            // Unconditional branches must also leave a protected region via
            // leave rather than br.
            foreach (var ins in instructions.ToArray())
            {
                if (ins.OpCode.Code is not (Code.Br or Code.Br_S) ||
                    ins.Operand is not Instruction brTarget)
                {
                    continue;
                }

                var index = instructions.IndexOf(ins);
                var targetIndex = instructions.IndexOf(brTarget);

                // A jump over a try block that starts immediately after this
                // branch is the flattened representation of entering the try
                // region. Leaving it unreachable makes the JIT reject the
                // method, so fall through into the try instead.
                var next = ins.Next;
                var entersTry = false;
                foreach (var eh in body.ExceptionHandlers)
                {
                    if (next == eh.TryStart)
                    {
                        var tryEnd = eh.TryEnd == null
                            ? instructions.Count
                            : instructions.IndexOf(eh.TryEnd);
                        var tryStart = instructions.IndexOf(eh.TryStart);
                        if (targetIndex < tryStart || targetIndex >= tryEnd)
                        {
                            entersTry = true;
                            break;
                        }
                    }
                }
                if (entersTry)
                {
                    ins.OpCode = OpCodes.Nop;
                    ins.Operand = null;
                    continue;
                }

                foreach (var eh in body.ExceptionHandlers)
                {
                    var tryStart = instructions.IndexOf(eh.TryStart);
                    var tryEnd = eh.TryEnd == null
                        ? instructions.Count
                        : instructions.IndexOf(eh.TryEnd);
                    var handlerStart = eh.HandlerStart == null
                        ? -1
                        : instructions.IndexOf(eh.HandlerStart);
                    var handlerEnd = eh.HandlerEnd == null
                        ? instructions.Count
                        : instructions.IndexOf(eh.HandlerEnd);
                    var inTry = index >= tryStart && index < tryEnd;
                    var inHandler = handlerStart >= 0 && index >= handlerStart && index < handlerEnd;
                    if (!inTry && !inHandler)
                    {
                        continue;
                    }
                    var targetInTry = targetIndex >= tryStart && targetIndex < tryEnd;
                    var targetInHandler = handlerStart >= 0 &&
                        targetIndex >= handlerStart && targetIndex < handlerEnd;
                    if ((inTry && !targetInTry) || (inHandler && !targetInHandler))
                    {
                        ins.OpCode = OpCodes.Leave;
                        break;
                    }
                }
            }

            // A ret instruction is not allowed inside a protected region.
            // Redirect such returns through a leave to a shared ret stub that
            // is appended after all handlers.
            Instruction? retStub = null;
            foreach (var ins in instructions.ToArray())
            {
                if (ins.OpCode.Code != Code.Ret)
                {
                    continue;
                }

                var index = instructions.IndexOf(ins);
                var inside = false;
                foreach (var eh in body.ExceptionHandlers)
                {
                    var tryStart = instructions.IndexOf(eh.TryStart);
                    var tryEnd = eh.TryEnd == null
                        ? instructions.Count
                        : instructions.IndexOf(eh.TryEnd);
                    var handlerStart = eh.HandlerStart == null
                        ? -1
                        : instructions.IndexOf(eh.HandlerStart);
                    var handlerEnd = eh.HandlerEnd == null
                        ? instructions.Count
                        : instructions.IndexOf(eh.HandlerEnd);
                    if ((index >= tryStart && index < tryEnd) ||
                        (handlerStart >= 0 && index >= handlerStart && index < handlerEnd))
                    {
                        inside = true;
                        break;
                    }
                }

                if (!inside)
                {
                    continue;
                }

                if (retStub == null)
                {
                    retStub = Instruction.Create(OpCodes.Ret);
                    instructions.Add(retStub);
                }
                ins.OpCode = OpCodes.Leave;
                ins.Operand = retStub;
            }
        }
    }
}
