using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Pseudocode.Data;
using HashlinkNET.Compiler.Steps;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode.Steps.Backend
{
    /// <summary>
    /// IL-level stack forwarding: when a local register is read exactly once,
    /// is not live-out of its block (no successor uses it before assigning),
    /// and the intervening instructions keep the evaluation stack balanced,
    /// the <c>stloc</c>/<c>ldloc</c> pair is removed so the value stays on the
    /// evaluation stack until its use.
    /// <para>
    /// Functions with exception handlers are skipped because protected-region
    /// boundaries constrain the evaluation stack.
    /// </para>
    /// </summary>
    internal class StackStoreEliminationStep : CompileStep
    {
        private const int MaxScan = 512;

        public override void Execute( IDataContainer container )
        {
            var gdata = container.GetGlobalData<FuncEmitGlobalData>();
            if (gdata.TrapRegions.Count != 0)
            {
                return;
            }

            var md = gdata.Definition;
            var blocks = container.GetGlobalData<List<IRBasicBlockData>>();
            var addressTaken = CollectAddressTaken(md);
            var joinPoints = CollectJoinPoints(md);

            foreach (var bb in blocks)
            {
                var liveOut = bb.liveOutRegs;
                if (liveOut is null)
                {
                    continue;
                }

                var instructions = new List<Instruction>();
                for (var it = bb.startInst.Next; it is not null && it != bb.endInst; it = it.Next)
                {
                    instructions.Add(it);
                }

                if (HasShortLocalForm(instructions))
                {
                    continue;
                }

                for (var i = 0; i < instructions.Count; i++)
                {
                    var store = instructions[i];
                    if (store.OpCode.Code != Code.Stloc ||
                        store.Operand is not VariableDefinition variable ||
                        addressTaken.Contains(variable) ||
                        variable.Index >= gdata.LocalRegisters.Count)
                    {
                        continue;
                    }
                    var reg = gdata.LocalRegisters[variable.Index];
                    if (reg is null || reg.IsExposed)
                    {
                        continue;
                    }

                    var delta = 0;
                    Instruction? load = null;
                    var foundNextStore = false;
                    var failed = false;
                    var scanned = 0;

                    for (var j = i + 1; j < instructions.Count; j++)
                    {
                        if (++scanned > MaxScan)
                        {
                            failed = true;
                            break;
                        }

                        var ins = instructions[j];
                        var code = ins.OpCode.Code;

                        // A branch may enter the span without executing the
                        // store; the value would then be missing on the stack.
                        if (load is null && joinPoints.Contains(ins))
                        {
                            failed = true;
                            break;
                        }

                        if (code == Code.Stloc && ins.Operand is VariableDefinition other)
                        {
                            if (ReferenceEquals(other, variable))
                            {
                                foundNextStore = true;
                                break;
                            }
                            if (load is null)
                            {
                                if (delta < 1)
                                {
                                    failed = true;
                                    break;
                                }
                                delta--;
                            }
                            continue;
                        }

                        if (code == Code.Ldloc && ins.Operand is VariableDefinition used)
                        {
                            if (ReferenceEquals(used, variable))
                            {
                                if (load is not null || delta != 0)
                                {
                                    failed = true;
                                    break;
                                }
                                load = ins;
                                continue;
                            }
                            if (load is null)
                            {
                                delta++;
                            }
                            continue;
                        }

                        if (code == Code.Ldloca &&
                            ins.Operand is VariableDefinition addressOf &&
                            ReferenceEquals(addressOf, variable))
                        {
                            failed = true;
                            break;
                        }

                        if (IsControlFlow(code))
                        {
                            break;
                        }

                        if (load is not null)
                        {
                            continue;
                        }

                        if (!TryGetStackOperate(ins, md, out var pop, out var push))
                        {
                            failed = true;
                            break;
                        }
                        // The instruction must not pop below the preserved value.
                        if (delta < pop)
                        {
                            failed = true;
                            break;
                        }
                        delta += push - pop;
                    }

                    if (failed || load is null)
                    {
                        continue;
                    }

                    // If the value can still be read by a successor before being
                    // assigned, it cannot stay on the stack past the block.
                    if (!foundNextStore && liveOut.Contains(reg))
                    {
                        continue;
                    }

                    store.OpCode = OpCodes.Nop;
                    store.Operand = null;
                    load.OpCode = OpCodes.Nop;
                    load.Operand = null;
                }
            }
        }

        private static HashSet<VariableDefinition> CollectAddressTaken( MethodDefinition md )
        {
            var result = new HashSet<VariableDefinition>();
            foreach (var ins in md.Body.Instructions)
            {
                if (ins.OpCode.Code is Code.Ldloca or Code.Ldloca_S &&
                    ins.Operand is VariableDefinition variable)
                {
                    result.Add(variable);
                }
            }
            return result;
        }

        private static HashSet<Instruction> CollectJoinPoints( MethodDefinition md )
        {
            var result = new HashSet<Instruction>();
            foreach (var ins in md.Body.Instructions)
            {
                switch (ins.Operand)
                {
                    case Instruction target:
                        result.Add(target);
                        break;
                    case Instruction[] targets:
                        foreach (var t in targets)
                        {
                            result.Add(t);
                        }
                        break;
                }
            }
            foreach (var handler in md.Body.ExceptionHandlers)
            {
                Add(handler.TryStart);
                Add(handler.TryEnd);
                Add(handler.HandlerStart);
                Add(handler.HandlerEnd);
                Add(handler.FilterStart);
            }
            return result;

            void Add( Instruction? instruction )
            {
                if (instruction is not null)
                {
                    result.Add(instruction);
                }
            }
        }

        private static bool HasShortLocalForm( List<Instruction> instructions )
        {
            foreach (var ins in instructions)
            {
                var code = ins.OpCode.Code;
                if (code is Code.Ldloc_0 or Code.Ldloc_1 or Code.Ldloc_2 or Code.Ldloc_3
                    or Code.Stloc_0 or Code.Stloc_1 or Code.Stloc_2 or Code.Stloc_3
                    or Code.Ldloc_S or Code.Stloc_S or Code.Ldloca_S)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsControlFlow( Code code )
        {
            return code is Code.Br or Code.Br_S or Code.Leave or Code.Leave_S
                or Code.Ret or Code.Throw or Code.Rethrow or Code.Switch
                or Code.Endfinally or Code.Endfilter or Code.Jmp;
        }

        private static bool TryGetStackOperate( Instruction inst, MethodDefinition md,
            out int pop, out int push )
        {
            pop = 0;
            push = 0;
            var op = inst.OpCode;
            var code = op.Code;

            if (code is Code.Call or Code.Callvirt or Code.Newobj or Code.Calli)
            {
                bool hasThis;
                bool returnsValue;
                int arguments;

                if (code == Code.Calli)
                {
                    if (inst.Operand is not CallSite callSite)
                    {
                        return false;
                    }
                    arguments = callSite.Parameters.Count;
                    hasThis = callSite.HasThis;
                    returnsValue = callSite.ReturnType.FullName != "System.Void";
                }
                else
                {
                    if (inst.Operand is not MethodReference method ||
                        method.CallingConvention == MethodCallingConvention.VarArg)
                    {
                        return false;
                    }
                    arguments = method.Parameters.Count;
                    hasThis = method.HasThis;
                    returnsValue = method.ReturnType.FullName != "System.Void";
                }

                if (code == Code.Newobj)
                {
                    pop = arguments;
                    push = 1;
                    return true;
                }

                // calli additionally pops the function pointer on top of the
                // arguments.
                pop = arguments + (hasThis ? 1 : 0) + (code == Code.Calli ? 1 : 0);
                push = returnsValue ? 1 : 0;
                return true;
            }

            if (code == Code.Ret)
            {
                pop = md.ReturnType.FullName == "System.Void" ? 0 : 1;
                push = 0;
                return true;
            }

            if (op.StackBehaviourPush == StackBehaviour.Varpush ||
                op.StackBehaviourPop == StackBehaviour.Varpop ||
                op.StackBehaviourPop == StackBehaviour.PopAll)
            {
                return false;
            }

            push = op.StackBehaviourPush switch
            {
                StackBehaviour.Push0 => 0,
                StackBehaviour.Push1_push1 => 2,
                _ => 1
            };
            pop = op.StackBehaviourPop switch
            {
                StackBehaviour.Pop0 => 0,
                StackBehaviour.Pop1 => 1,
                StackBehaviour.Popi => 1,
                StackBehaviour.Popref => 1,
                StackBehaviour.Popi_popi_popi => 3,
                >= StackBehaviour.Pop1_pop1 and <= StackBehaviour.Popi_popr8 => 2,
                StackBehaviour.Popref_pop1 or StackBehaviour.Popref_popi => 2,
                >= StackBehaviour.Popref_popi_popi and <= StackBehaviour.Popref_popi_popref => 3,
                _ => -1
            };
            return pop >= 0;
        }
    }
}
