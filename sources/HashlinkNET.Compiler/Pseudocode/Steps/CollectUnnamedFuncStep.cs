using HashlinkNET.Compiler.Data;
using Mono.Cecil;
using System.Diagnostics;

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

            var entry = gdata.Code.GetFunctionById(gdata.Code.Entrypoint);

            Debug.Assert(entry != null);

            container.GetData<MethodDefinition>(entry).Name = "Entrypoint";
        }
    }
}
