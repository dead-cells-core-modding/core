using HashlinkNET.Compiler.Pseudocode.Data;
using HashlinkNET.Compiler.Pseudocode.IR;
using HashlinkNET.Compiler.Steps;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.Steps.Backend
{
    internal class FillBytecodeMappingDataStep : CompileStep
    {
        public override void Execute( IDataContainer container )
        {
            var gdata = container.GetGlobalData<FuncEmitGlobalData>();
            var md = gdata.Definition;
            var insts = md.Body.Instructions;

            for (int i = 0; i < insts.Count; i++)
            {
                var inst = insts[i];
                if (inst.OpCode == OpCodes.Nop &&
                    inst.Operand is IR_DebugSequence info)
                {
                    inst.Operand = null;
                    gdata.MappingData.Instructions.Add(new()
                    {
                        ILIndex = i,
                        Line = info.Info.Line,
                        Path = info.Info.FileName
                    });
                }
            }
        }
    }
}
