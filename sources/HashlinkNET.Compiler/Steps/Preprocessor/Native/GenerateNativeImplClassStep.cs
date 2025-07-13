using HashlinkNET.Compiler.Data;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Steps.Preprocessor.Native
{
    internal class GenerateNativeImplClassStep : CompileStep
    {
        public override void Execute( IDataContainer container )
        {
            var gdata = container.GetGlobalData<GlobalData>();
            var module = gdata.Module;
            var dict = new Dictionary<string, TypeDefinition>();

            foreach (var v in gdata.Code.Natives)
            {
                if (dict.ContainsKey(v.Lib))
                {
                    continue;
                }
                var td = new TypeDefinition("HashlinkNET.Native.Impl", "Lib_" + v.Lib, TypeAttributes.Class 
                    | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Abstract, module.TypeSystem.Object);
                module.Types.Add(td);
                dict.Add(v.Lib, td);
            }

            container.AddGlobalData(new NativeImplClasses(
                NativeImplClass: dict
            ));
        }
    }
}
