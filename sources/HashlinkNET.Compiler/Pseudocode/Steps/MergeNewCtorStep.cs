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

            var hashset = new HashSet<int>();

            foreach (var bb in gdata.IRBasicBlocks)
            {
                hashset.Clear();

                var list = bb.ir;
                for (var i = 0; i < list.Count; i++)
                {
                    if (hashset.Contains(i))
                    {
                        continue;
                    }
                    if (list[i].IR is not IR_SetLocalReg setIr ||
                        setIr.src.IR is not IR_New newIr ||
                        setIr.dst == null)
                    {
                        continue;
                    }


                    for (int j = i; j < bb.ir.Count; j++)
                    {
                        if (list[j].IR is not IR_SetLocalReg call_slr ||
                            call_slr.src.IR is not IR_Call call)
                        {
                            continue;
                        }
                        if (call.args.Length == 0)
                        {
                            continue;
                        }
                        if (call.method.Name != "s___constructor__")
                        {
                            continue;
                        }
                        if (call.args[0].IR is not IR_LoadLocalReg llr)
                        {
                            continue;
                        }
                        if (llr.src != setIr.dst)
                        {
                            continue;
                        }

                        hashset.Add(j);
                        list[i].IR = null;
                        list[j].IR = new IR_SetLocalReg(setIr.dst,
                            new IR_New(newIr.type, call.args[1..]));
                        break;
                    }
                }
            }
        }
    }
}
