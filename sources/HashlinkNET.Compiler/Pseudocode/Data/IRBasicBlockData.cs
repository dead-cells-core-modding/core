
using HashlinkNET.Compiler.Pseudocode.IR;
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
        public readonly Instruction startInst = Instruction.Create(OpCodes.Nop);
        public readonly List<Transition> transitions = []; 
        public IRBasicBlockData? defaultTransition;
        public readonly List<IRResult> ir = [];
        public int startInHlbc;
        public int index;

        public RegisterAccessData? registerAccessData;

        public IRResult[]? flatIR;

        public void GenerateFlatIR()
        {
            var count = 0;
            static void CountIRResults( IRResult ir, ref int count )
            {
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
            static void ForeachIR( IRResult ir, IRResult[] array, ref int index )
            {
                if (ir.IsNone)
                {
                    return;
                }
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
