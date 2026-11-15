using HashlinkNET.Compiler.Pseudocode.Data;
using HashlinkNET.Compiler.Steps;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode.Steps.Backend
{
    /// <summary>
    /// Removes a value-producing instruction immediately before a branch
    /// target when that value would make the target's stack height differ
    /// from its other predecessors. Such pushes come from lowered expressions
    /// that are not actually consumed on the branch edge.
    /// </summary>
    internal class RemoveDeadPushBeforeTargetStep : CompileStep
    {
        public override void Execute( IDataContainer container )
        {
            var gdata = container.GetGlobalData<FuncEmitGlobalData>();
            var body = gdata.Definition.Body;
            var list = body.Instructions;
            if (list.Count < 2)
            {
                return;
            }

            var targets = new HashSet<Instruction>();
            foreach (var ins in list)
            {
                if (ins.Operand is Instruction t)
                {
                    targets.Add(t);
                }
                else if (ins.Operand is Instruction[] ts)
                {
                    foreach (var x in ts)
                    {
                        targets.Add(x);
                    }
                }
            }

            for (var i = 1; i < list.Count; i++)
            {
                var target = list[i];
                if (!targets.Contains(target))
                {
                    continue;
                }
                var prev = list[i - 1];
                if (targets.Contains(prev))
                {
                    continue;
                }
                if (IsPurePush(prev.OpCode.Code))
                {
                    prev.OpCode = OpCodes.Nop;
                    prev.Operand = null;
                }
            }
        }

        private static bool IsPurePush( Code code ) => code is
            Code.Ldarg or Code.Ldarg_0 or Code.Ldarg_1 or Code.Ldarg_2 or Code.Ldarg_3 or
            Code.Ldarg_S or Code.Ldarga or Code.Ldarga_S or
            Code.Ldloc or Code.Ldloc_0 or Code.Ldloc_1 or Code.Ldloc_2 or Code.Ldloc_3 or
            Code.Ldloc_S or Code.Ldloca or Code.Ldloca_S or
            Code.Ldc_I4 or Code.Ldc_I4_M1 or Code.Ldc_I4_0 or Code.Ldc_I4_1 or
            Code.Ldc_I4_2 or Code.Ldc_I4_3 or Code.Ldc_I4_4 or Code.Ldc_I4_5 or
            Code.Ldc_I4_6 or Code.Ldc_I4_7 or Code.Ldc_I4_8 or Code.Ldc_I4_S or
            Code.Ldc_I8 or Code.Ldc_R4 or Code.Ldc_R8 or Code.Ldnull or Code.Ldstr or
            Code.Ldsfld or Code.Ldsflda;
    }
}
