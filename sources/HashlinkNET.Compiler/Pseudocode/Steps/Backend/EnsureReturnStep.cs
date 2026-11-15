using HashlinkNET.Compiler.Pseudocode.Data;
using HashlinkNET.Compiler.Steps;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode.Steps.Backend
{
    /// <summary>
    /// Last-resort guard for lowering cases that can end a block with a value
    /// but without a terminator. Emits a default return and discards any
    /// remaining value from the expression.
    /// </summary>
    internal class EnsureReturnStep : CompileStep
    {
        public override void Execute( IDataContainer container )
        {
            var gdata = container.GetGlobalData<FuncEmitGlobalData>();
            var body = gdata.Definition.Body;
            var list = body.Instructions;
            if (list.Count == 0)
            {
                return;
            }

            var last = list[^1];
            if (last.OpCode.Code is Code.Ret or Code.Throw or Code.Rethrow or
                Code.Br or Code.Br_S or Code.Leave or Code.Leave_S or Code.Jmp)
            {
                return;
            }

            var hasValue = Delta(last) > 0;
            var ret = gdata.Definition.ReturnType;
            if (ret.MetadataType == MetadataType.Void)
            {
                if (hasValue)
                {
                    list.Add(Instruction.Create(OpCodes.Pop));
                }
                list.Add(Instruction.Create(OpCodes.Ret));
                return;
            }

            if (!hasValue)
            {
                var local = new VariableDefinition(ret);
                body.Variables.Add(local);
                body.InitLocals = true;
                list.Add(Instruction.Create(OpCodes.Ldloca, local));
                list.Add(Instruction.Create(OpCodes.Initobj, ret));
                list.Add(Instruction.Create(OpCodes.Ldloc, local));
            }
            list.Add(Instruction.Create(OpCodes.Ret));
        }

        private static int Delta( Instruction ins )
        {
            var c = ins.OpCode.Code;
            if (ins.Operand is MethodReference mr)
            {
                if (c == Code.Newobj)
                {
                    return 1 - mr.Parameters.Count;
                }
                if (c is Code.Call or Code.Callvirt)
                {
                    return (mr.ReturnType.MetadataType == MetadataType.Void ? 0 : 1) -
                        (mr.Parameters.Count + (mr.HasThis ? 1 : 0));
                }
            }
            return c switch
            {
                Code.Ldc_I4 or Code.Ldc_I4_S or Code.Ldc_I8 or Code.Ldc_R4 or Code.Ldc_R8 or
                Code.Ldnull or Code.Ldstr or Code.Ldloc or Code.Ldloc_S or Code.Ldarg or
                Code.Ldarg_S or Code.Ldsfld or Code.Ldsflda or Code.Ldftn => 1,
                Code.Stloc or Code.Stloc_S or Code.Starg or Code.Starg_S or Code.Stsfld or Code.Pop => -1,
                Code.Add or Code.Sub or Code.Mul or Code.Div or Code.And or Code.Or or Code.Xor => -1,
                Code.Brtrue or Code.Brfalse => -1,
                _ => 0
            };
        }
    }
}
