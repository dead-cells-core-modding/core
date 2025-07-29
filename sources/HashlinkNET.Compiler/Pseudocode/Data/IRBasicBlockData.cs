
using HashlinkNET.Compiler.Pseudocode.Data.DFA;
using HashlinkNET.Compiler.Pseudocode.IR;
using HashlinkNET.Compiler.Pseudocode.IR.FlowControl;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.Data
{
    class IRBasicBlockData
    {
        public readonly Instruction endInst = Instruction.Create(OpCodes.Nop);
        public readonly Instruction startInst = Instruction.Create(OpCodes.Nop);
        public readonly List<IRBasicBlockData> parents = [];
        public readonly List<Transition> transitions = []; 
        public IRBasicBlockData? defaultTransition;
        public readonly List<IRResult> ir = [];
        public int startInHlbc;
        public int index;

        public RegisterAccessData? registerAccessData;

        public IRResult[]? flatIR;

        public bool CanRepeat => transitions.Count <= 1 && parents.Count > 1 && ir.Count <= 20 && ir.Count > 0;

        public void ReserveConditionalJmp()
        {
            if (ir.Count == 0 ||
                transitions.Count != 2 ||
                defaultTransition == null ||
                ir[^1].IR is not IIR_JmpConditional jmp)
            {
                throw new InvalidOperationException();
            }

            var defaultTarget = defaultTransition;
            var condTarget = jmp.Target;

            jmp.ReserveCondition();
            jmp.Target = defaultTarget;

            defaultTransition = condTarget;

            transitions.Clear();
            transitions.Add(new(defaultTarget, TransitionKind.Conditional));
            transitions.Add(new(condTarget, TransitionKind.Default));
        }


        public IRBasicBlockData Clone()
        {
            var result = new IRBasicBlockData()
            {
                startInHlbc = startInHlbc,
                index = index,
                defaultTransition = defaultTransition,
                registerAccessData = registerAccessData,
                flatIR = flatIR
            };
            result.parents.AddRange(parents);
            result.transitions.AddRange(transitions);
            result.ir.AddRange(ir);
            return result;
        }

        public void GenerateFlatIR()
        {
            var count = 0;
            static void CountIRResults( IRResult? ir, ref int count )
            {
                if (ir == null)
                {
                    return;
                }
                if (ir.IsNone)
                {
                    return;
                }
                count++;
                foreach (var v in ir.IR!.Values)
                {
                    CountIRResults(v, ref count);
                }
            }
            foreach (var v in ir)
            {
                CountIRResults(v, ref count);
            }
            var result = new IRResult[count];
            static void ForeachIR( IRResult? ir, IRResult[] array, ref int index )
            {
                if (ir == null)
                {
                    return;
                }
                if (ir.IsNone)
                {
                    return;
                }
                ir.Index = index;
                array[index++] = ir;
                foreach (var v in ir.IR!.Values)
                {
                    ForeachIR(v, array, ref index);
                }
            }
            var index = 0;
            foreach (var v in ir)
            {
                ForeachIR(v, result, ref index);
            }
            flatIR = result;
        }

        public void AddIR( IRBase ir )
        {
            this.ir.Add(ir);
        }

        public record class Transition
        (
            IRBasicBlockData Target,
            TransitionKind Kind
        );
    }
}
