using HashlinkNET.Compiler.Pseudocode.Data;
using HashlinkNET.Compiler.Pseudocode.IR;
using HashlinkNET.Compiler.Pseudocode.IR.Call;
using HashlinkNET.Compiler.Pseudocode.IR.Object;
using HashlinkNET.Compiler.Steps;

namespace HashlinkNET.Compiler.Pseudocode.Steps
{
    /// <summary>
    /// Merges an <c>ONew</c> followed by its <c>__inst_construct__</c> call
    /// into a single parameterized constructor call, so the emitted IL
    /// decompiles to a natural <c>new SomeType(args)</c> expression.
    /// </summary>
    internal class MergeNewCtorStep : CompileStep
    {
        public override void Execute( IDataContainer container )
        {
            var gdata = container.GetGlobalData<FuncEmitGlobalData>();

            foreach (var bb in gdata.IRBasicBlocks)
            {
                var list = bb.ir;
                for (var i = 0; i < list.Count; i++)
                {
                    if (list[i].IR is not IR_SetLocalReg setIr ||
                        setIr.src.IR is not IR_New newIr ||
                        newIr.ctorArgs.Length != 0 ||
                        setIr.dst == null)
                    {
                        continue;
                    }

                    var j = i + 1;
                    while (j < list.Count && list[j].IR is IR_DebugSequence)
                    {
                        j++;
                    }
                    if (j >= list.Count)
                    {
                        continue;
                    }

                    IR_Call? call = list[j].IR as IR_Call;
                    if (call == null && list[j].IR is IR_SetLocalReg callStore)
                    {
                        call = callStore.src.IR as IR_Call;
                    }
                    if (call == null ||
                        call.method.Name != "__inst_construct__" ||
                        call.args.Length == 0 ||
                        call.args[0].IR is not IR_LoadLocalReg self ||
                        self.src != setIr.dst)
                    {
                        continue;
                    }

                    setIr.src.IR = new IR_New(newIr.type, call.args[1..]);
                    list.RemoveAt(j);
                }
            }
        }
    }
}
