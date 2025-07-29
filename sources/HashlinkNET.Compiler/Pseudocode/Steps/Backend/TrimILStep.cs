using HashlinkNET.Compiler.Pseudocode.Data;
using HashlinkNET.Compiler.Steps;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.Steps.Backend
{
    internal class TrimILStep : CompileStep
    {
        private static readonly object REMOVED_NOP = new();
        private record struct StackItem(StlocInfo? Info, Instruction Instruction);
        private class StlocInfo
        {
            public required HlFuncRegisterData? register;
            public required Instruction instruction;
            public int used = 0;
            public bool blocked = false;
            public bool isLast = false;
            public bool isConstant = false;
            public object? constant = null;
            public required VariableDefinition variable;
        }
        private static bool TryGetConstant( Instruction inst, out object? value )
        {
            var op = inst.OpCode.Code;
            if (op == Code.Ldc_I4 ||
                op == Code.Ldc_I8 ||
                op == Code.Ldc_R4 ||
                op == Code.Ldc_R8 ||
                op == Code.Ldtoken ||
                op == Code.Ldstr ||
                op == Code.Ldnull)
            {
                value = inst.Operand;
                return true;
            }
            value = null;
            return false;
        }
        private static void SetConstant( Instruction inst, object? value )
        {
            inst.Operand = value;
            if (value == null)
            {
                inst.OpCode = OpCodes.Ldnull;
            }
            else if (value is int)
            {
                inst.OpCode = OpCodes.Ldc_I4;
            }
            else if (value is long)
            {
                inst.OpCode = OpCodes.Ldc_I8;
            }
            else if (value is float)
            {
                inst.OpCode = OpCodes.Ldc_R4;
            }
            else if (value is double)
            {
                inst.OpCode = OpCodes.Ldc_R8;
            }
            else if (value is MemberReference)
            {
                inst.OpCode = OpCodes.Ldtoken;
            }
            else if (value is string)
            {
                inst.OpCode = OpCodes.Ldstr;
            }
            else
            {
                throw new InvalidOperationException();
            }
               
        }
        private (int pop, int push) GetStackOperate( Instruction inst, MethodDefinition md )
        {
            var op = inst.OpCode;
            var code = op.Code;
            var ope = inst.Operand!;
            if (code == Code.Call ||
                code == Code.Callvirt ||
                code == Code.Newobj)
            {
                var mr = (MethodReference)ope;
                var popnum = mr.Parameters.Count;
                var pushnum = 0;
                if (mr.ReturnType.FullName != "System.Void")
                {
                    pushnum++;
                }
                if (mr.HasThis)
                {
                    popnum++;
                }
                if (code == Code.Newobj)
                {
                    popnum--;
                    pushnum++;
                }
                return (popnum, pushnum);
            }
            if (code == Code.Ret)
            {
                return (md.ReturnType.FullName == "System.Void" ? 0 : 1, 0);
            }
            if (op.StackBehaviourPush == StackBehaviour.Varpush ||
                op.StackBehaviourPop == StackBehaviour.Varpop)
            {
                throw new NotSupportedException();
            }
            var pop = 0;
            var push = 0;
            push += op.StackBehaviourPush switch
            {
                StackBehaviour.Push0 => 0,
                StackBehaviour.Push1_push1 => 2,
                _ => 1
            };
            pop += op.StackBehaviourPop switch
            {
                StackBehaviour.Pop0 => 0,
                StackBehaviour.Pop1 => 1,
                StackBehaviour.Popi => 1,
                StackBehaviour.Popref => 1,
                StackBehaviour.Popi_popi_popi => 3,
                >= StackBehaviour.Pop1_pop1 and <= StackBehaviour.Popi_popr8 => 2,
                StackBehaviour.Popref_pop1 or StackBehaviour.Popref_popi => 2,
                >= StackBehaviour.Popref_popi_popi and <= StackBehaviour.Popref_popi_popref => 3,
                _ => throw new InvalidOperationException()
            };
            return (pop, push);
        }
        public override void Execute( IDataContainer container )
        {
            var gdata = container.GetGlobalData<FuncEmitGlobalData>();
            var list = container.GetGlobalData<List<IRBasicBlockData>>();
            var md = gdata.Definition;

            var stack = new List<StackItem>();
            int stack_id;

            void Push( Instruction instruction, StlocInfo? inst )
            {
                if (stack.Count == stack_id)
                {
                    stack.Add(new(inst, instruction));
                }
                else
                {
                    stack[stack_id] = new(inst, instruction);
                }
                stack_id++;
            }
            StackItem Pop()
            {
                Debug.Assert(stack_id > 0);
                stack_id--;
                while (stack[stack_id].Info != null && stack_id > 0)
                    stack_id--;
                var result = stack[stack_id];
                stack[stack_id] = default;
                return result;
            }


            foreach (var bb in list)
            {
                var rad = bb.registerAccessData!;
                var it = bb.startInst;
                var buf = new StlocInfo?[md.Body.Variables.Count];

                while (it != bb.endInst)
                {
                    if (it.OpCode == OpCodes.Stloc)
                    {
                        var loc = (VariableDefinition)it.Operand;
                        var info = new StlocInfo()
                        {
                            instruction = it,
                            variable = loc,
                            register = gdata.LocalRegisters[loc.Index],
                            isLast = true
                        };
                        Debug.Assert(info.register?.Variable == loc);
                        it.Operand = info;
                        if (buf[loc.Index] != null)
                        {
                            buf[loc.Index]!.isLast = false;
                        }
                        buf[loc.Index] = info;
                    }
                    else if (it.OpCode == OpCodes.Ldloc)
                    {
                        var st = buf[((VariableDefinition)it.Operand).Index];
                        if (st != null)
                        {
                            st.used++;
                            it.Operand = st;
                        }
                    }
                    it = it.Next;
                }

                it = bb.startInst;

                stack.Clear();
                stack_id = 0;

                while (it != bb.endInst)
                {
                    var op = it.OpCode;
                    //goto SKIP;
                    if (op == OpCodes.Stloc)
                    {
                        var info = (StlocInfo)it.Operand;
                        if ((info.register?.IsExposed ?? true) ||
                            rad.readReg[info.register.Index] != rad.writeReg[info.register.Index] ||
                            (rad.exposedReg[info.register.Index] && info.isLast))
                        {
                            goto SKIP;
                        }
                        var val = Pop();

                        info.isConstant = TryGetConstant(val.Instruction, out info.constant);

                        if (info.isConstant)
                        {
                            val.Instruction.OpCode = OpCodes.Nop;
                            val.Instruction.Operand = REMOVED_NOP;
                            it.OpCode = OpCodes.Nop;
                            it.Operand = REMOVED_NOP;
                        }
                        else
                        {
                            Push(it, info);
                        }

                        goto FINALLY;
                    }
                    else if (op == OpCodes.Ldloc)
                    {
                        if (it.Operand is not StlocInfo info ||
                            info.blocked ||
                            info.used > 1)
                        {
                            goto SKIP;
                        }
                        for (var i = stack_id - 1; i >= 0; i--)
                        {
                            if (info.isConstant)
                            {
                                SetConstant(it, info.constant);
                                Push(it, null);
                                goto FINALLY;
                            }
                            if (stack[i].Info == null)
                            {
                                info.blocked = true;
                                goto SKIP;
                            }
                            if (stack[i].Info == info)
                            {
                                it.Operand = REMOVED_NOP;
                                it.OpCode = OpCodes.Nop;
                                info.instruction.OpCode = OpCodes.Nop;
                                info.instruction.Operand = REMOVED_NOP;

                                stack[i] = stack[i] with
                                {
                                    Info = null
                                };
                                /*stack_id--;
                                while (stack[stack_id] != null && stack_id > 0)
                                    stack_id--;

                                Push(null);*/
                                goto FINALLY;
                            }
                        }
                        goto SKIP;
                    }
                    SKIP:
                    (var pop, var push) = GetStackOperate(it, md);
                    for (var i = 0; i < pop; i++)
                    {
                        Pop();
                    }
                    for (var i = 0; i < push; i++)
                    {
                        Push(it, null);
                    }
                    FINALLY:
                    it = it.Next;
                }

                it = bb.startInst.Next;

                while (it != bb.endInst)
                {
                    if (it.Operand is StlocInfo info)
                    {
                        it.Operand = info.variable;
                    }
                    it = it.Next;
                }
            }

            // 

            {
                Instruction? lastBr = null;
                foreach (var v in md.Body.Instructions)
                {
                    if (v.OpCode == OpCodes.Br)
                    {
                        lastBr = v;
                        continue;
                    }
                    if (lastBr != null)
                    {
                        if (v == lastBr.Operand)
                        {
                            lastBr.Operand = REMOVED_NOP;
                            lastBr.OpCode = OpCodes.Nop;
                            continue;
                        }
                    }
                    if (v.OpCode != OpCodes.Nop)
                    {
                        lastBr = null;
                    }
                    
                }

                var array = md.Body.Instructions.ToArray();
                md.Body.Instructions.Clear();
                foreach (var v in array)
                {
                    if (v.Operand == REMOVED_NOP)
                    {
                        continue;
                    }
                    md.Body.Instructions.Add(v);
                }
            }
        }
    }
}
