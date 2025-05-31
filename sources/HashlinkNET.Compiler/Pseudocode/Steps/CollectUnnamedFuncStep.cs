using HashlinkNET.Compiler.Data;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Steps.Func
{
    class CollectUnnamedFuncStep : CompileStep
    {
        public override void Execute( IDataContainer container )
        {
            var gdata = container.GetGlobalData<GlobalData>();

            TypeDefinition unnamedFunType = new("", "UnnamedFunctions", TypeAttributes.Class | TypeAttributes.Public
            //| TypeAttributes.Abstract | TypeAttributes.Sealed
            )
            {
                BaseType = gdata.Module.TypeSystem.Object
            };
            gdata.Module.Types.Add(unnamedFunType);

            foreach (var f in gdata.Code.Functions)
            {
                var md = container.GetData<MethodDefinition>(f);
                if (md.DeclaringType == null)
                {
                    unnamedFunType.Methods.Add(md);
                }
            }
        }
    }
}
