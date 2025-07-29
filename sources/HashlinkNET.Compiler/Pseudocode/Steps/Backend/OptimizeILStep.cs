using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Pseudocode.Data;
using HashlinkNET.Compiler.Steps;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.Steps.Backend
{
    class OptimizeILStep : CompileStep
    {
        public override void Execute( IDataContainer container )
        {
            var gdata = container.GetGlobalData<FuncEmitGlobalData>();
            var md = gdata.Definition;

            int offset = 0;
            foreach (var v in md.Body.Instructions)
            {
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
                    if (index < 256)
                    {
                        v.OpCode = index switch
                        {
                            0 => OpCodes.Ldarg_0,
                            1 => OpCodes.Ldarg_1,
                            2 => OpCodes.Ldarg_2,
                            3 => OpCodes.Ldarg_3,
                            _ => OpCodes.Ldarg_S
                        };
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
                    if (index < 256)
                    {
                        v.OpCode = index switch
                        {
                            0 => OpCodes.Ldloc_0,
                            1 => OpCodes.Ldloc_1,
                            2 => OpCodes.Ldloc_2,
                            3 => OpCodes.Ldloc_3,
                            _ => OpCodes.Ldloc_S
                        };
                    }
                }
                else if (code == Code.Stloc)
                {
                    var index = ((VariableDefinition)v.Operand!).Index;
                    if (index < 256)
                    {
                        v.OpCode = index switch
                        {
                            0 => OpCodes.Stloc_0,
                            1 => OpCodes.Stloc_1,
                            2 => OpCodes.Stloc_2,
                            3 => OpCodes.Stloc_3,
                            _ => OpCodes.Stloc_S
                        };
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
                    if (val < 128 && val > -128)
                    {
                        v.OpCode = val switch
                        {
                            0 => OpCodes.Ldc_I4_0,
                            1 => OpCodes.Ldc_I4_1,
                            2 => OpCodes.Ldc_I4_2,
                            3 => OpCodes.Ldc_I4_3,
                            4 => OpCodes.Ldc_I4_4,
                            5 => OpCodes.Ldc_I4_5,
                            6 => OpCodes.Ldc_I4_6,
                            7 => OpCodes.Ldc_I4_7,
                            8 => OpCodes.Ldc_I4_8,
                            _ => OpCodes.Ldc_I4_S
                        };
                        if (val > 8 || val < 0)
                        {
                            v.Operand = (sbyte)val;
                        }
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
