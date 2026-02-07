using HashlinkNET.Compiler.Pseudocode.Data;
using HashlinkNET.Compiler.Steps;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Diagnostics;

namespace HashlinkNET.Compiler.Pseudocode.Steps.Backend
{
    class OptimizeILStep : CompileStep
    {
        private readonly static Instruction[] Ldarg_s = [
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Ldarg_1),
            Instruction.Create(OpCodes.Ldarg_2),
            Instruction.Create(OpCodes.Ldarg_3),
            ];
        private readonly static Instruction[] Ldloc_s = [
            Instruction.Create(OpCodes.Ldloc_0),
            Instruction.Create(OpCodes.Ldloc_1),
            Instruction.Create(OpCodes.Ldloc_2),
            Instruction.Create(OpCodes.Ldloc_3),
            ];
        private readonly static Instruction[] Stloc_s = [
            Instruction.Create(OpCodes.Stloc_0),
            Instruction.Create(OpCodes.Stloc_1),
            Instruction.Create(OpCodes.Stloc_2),
            Instruction.Create(OpCodes.Stloc_3),
            ];
        private readonly static Instruction[] Ldc_I4_s = [
            .. Enumerable.Range(-127, 128 - 2).Where(x => x < 0).Select(x => Instruction.Create(OpCodes.Ldc_I4_S, (sbyte)x)),
            Instruction.Create(OpCodes.Ldc_I4_M1),
            Instruction.Create(OpCodes.Ldc_I4_0),
            Instruction.Create(OpCodes.Ldc_I4_1),
            Instruction.Create(OpCodes.Ldc_I4_2),
            Instruction.Create(OpCodes.Ldc_I4_3),
            Instruction.Create(OpCodes.Ldc_I4_4),
            Instruction.Create(OpCodes.Ldc_I4_5),
            Instruction.Create(OpCodes.Ldc_I4_6),
            Instruction.Create(OpCodes.Ldc_I4_7),
            Instruction.Create(OpCodes.Ldc_I4_8),
            .. Enumerable.Range(9, 128 - 9).Select(x => Instruction.Create(OpCodes.Ldc_I4_S, (sbyte)x))
            ];

        public override void Execute( IDataContainer container )
        {
            var gdata = container.GetGlobalData<FuncEmitGlobalData>();
            var md = gdata.Definition;

            int offset = 0;

            for (int i = 0; i < md.Body.Instructions.Count; i++)
            {
                var v = md.Body.Instructions[i];
#if DEBUG
                Debug.Assert(v is not
                {
                    OpCode.OperandType: OperandType.InlineBrTarget
                } || v.Operand is not Instruction brTarget || brTarget.OpCode == OpCodes.Nop);
#endif

                v.Offset = offset++;
                var code = v.OpCode.Code;
                if (code == Code.Ldarg)
                {
                    var index = ((ParameterDefinition)v.Operand!).Index;
                    if (md.HasThis)
                    {
                        if (v.Operand != md.Body.ThisParameter)
                        {
                            index++;
                        }
                    }
                    if (index >= 0)
                    {
                        if (index < Ldarg_s.Length)
                        {
                            md.Body.Instructions[i] = Ldarg_s[index];
                        }
                        else if (index < 256)
                        {
                            v.OpCode = OpCodes.Starg_S;
                        }
                    }
                }
                else if (code == Code.Starg)
                {
                    var index = ((ParameterDefinition)v.Operand!).Index;
                    if (index < 256)
                    {
                        v.OpCode = OpCodes.Starg_S;
                    }
                }
                else if (code == Code.Ldarga)
                {
                    var index = ((ParameterDefinition)v.Operand!).Index;
                    if (index < 256)
                    {
                        v.OpCode = OpCodes.Ldarga_S;
                    }
                }
                else if (code == Code.Ldloc)
                {
                    var index = ((VariableDefinition)v.Operand!).Index;
                    if (index >= 0)
                    {
                        if (index < Ldloc_s.Length)
                        {
                            md.Body.Instructions[i] = Ldloc_s[index];
                        }
                        else if (index < 256)
                        {
                            v.OpCode = OpCodes.Ldloc_S;
                        }
                    }
                }
                else if (code == Code.Stloc)
                {
                    var index = ((VariableDefinition)v.Operand!).Index;
                    if (index >= 0)
                    {
                        if (index < Stloc_s.Length)
                        {
                            md.Body.Instructions[i] = Stloc_s[index];
                        }
                        else if (index < 256)
                        {
                            v.OpCode = OpCodes.Stloc_S;
                        }
                    }
                }
                else if (code == Code.Ldloca)
                {
                    var index = ((VariableDefinition)v.Operand!).Index;
                    if (index < 256)
                    {
                        v.OpCode = OpCodes.Ldloca_S;
                    }
                }
                else if (code == Code.Ldc_I4)
                {
                    var val = (int)v.Operand!;
                    if (val + 127 < Ldc_I4_s.Length && val + 127 >= 0)
                    {
                        md.Body.Instructions[i] = Ldc_I4_s[val + 127];
                    }
                }
                if (v.OpCode.OperandType == OperandType.InlineNone)
                {
                    v.Operand = null;
                }
            }
        }
    }
}
