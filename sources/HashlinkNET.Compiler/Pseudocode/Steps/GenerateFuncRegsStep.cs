using HashlinkNET.Compiler.Pseudocode.Data;
using HashlinkNET.Compiler.Steps;
using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Pseudocode.Steps
{
    class GenerateFuncRegsStep : CompileStep
    {
        public override void Execute( IDataContainer container )
        {
            var gdata = container.GetGlobalData<FuncEmitGlobalData>();
            var f = gdata.Function;
            var fd = gdata.FuncType;
            var regs = gdata.Registers;
            var md = gdata.Definition;

            md.FixPIndex();
            md.Body.Variables.Clear();

            for (var i = 0; i < f.LocalVariables.Length; i++)
            {
                if (i < fd.Arguments.Length)
                {
                    //Param
                    regs.Add(new(HlFuncRegisterData.RegisterKind.Parameter, i)
                    {
                        Parameter = md.HasThis ? 
                            (i == 0 ?  md.Body.ThisParameter : md.Parameters[i - 1]) 
                            : md.Parameters[i]
                    });
                }
                else
                {
                    //LocalVar
                    
                    var hlv = f.LocalVariables[i].Value;
                    if (hlv.Kind == Bytecode.HlTypeKind.Void)
                    {
                        regs.Add(null);
                        continue;
                    }
                    gdata.VariablesCount++;
                    var lv = new VariableDefinition(
                        container.GetTypeRef(hlv)
                        );
                    md.Body.Variables.Add(lv);
                    var reg = new HlFuncRegisterData(HlFuncRegisterData.RegisterKind.LocalVar, i)
                    {
                        Variable = lv
                    };
                    Debug.Assert(lv.Index == gdata.LocalRegisters.Count);
                    gdata.LocalRegisters.Add(reg);
                    regs.Add(reg);
                   
                }

            }
            if (f.Assigns != null)
            {
                foreach (var v in f.Assigns)
                {
                    if (v.Index <= 0 ||
                        v.Name == null)
                    {
                        continue;
                    }

                    if (!gdata.Assigns.TryGetValue(v.Index , out var list))
                    {
                        list = [];
                        gdata.Assigns[v.Index] = list;
                    }
                    list.Add(v.Name);
                }
            }
        }
    }
}
