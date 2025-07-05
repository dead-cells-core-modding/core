using HashlinkNET.Compiler.Data;
using Mono.Cecil;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Steps
{
    abstract class GenerateTypeCompileStep : ForeachHlTypeCompileStep
    {
        public record struct AddTypeInfo(
            TypeDefinition Type,
            int Index
            );
        protected readonly ConcurrentBag<AddTypeInfo> addedTypes = []; 
        protected readonly ConcurrentBag<CustomAttribute> addAssemblyAttributes = [];

        protected override void PostProcessing( IDataContainer container )
        {
            base.PostProcessing(container);
            var gdata = container.GetGlobalData<GlobalData>();
            var rdata = container.GetGlobalData<RuntimeImports>();
            var module = gdata.Module;
            foreach (var v in addedTypes)
            {
                if ((v.Index & 0x80000000) == 0 ||
                    v.Index == -1)
                {
                    module.Types.Add(v.Type);
                }
                
                if (v.Index != -1)
                {
                    v.Type.CustomAttributes.Add(new(
                        rdata.attrTIndexCtor)
                    {
                        ConstructorArguments =
                            {
                                new(gdata.Module.TypeSystem.Int32, v.Index)
                            }
                    });
                    module.Assembly.CustomAttributes.Add(new(
                        rdata.attrTypeBindingCtor
                        )
                    {
                        ConstructorArguments =
                        {
                            new(gdata.Module.TypeSystem.Int32, v.Index),
                            new(gdata.Module.TypeSystem.TypedReference, v.Type)
                        }
                    });
                }
            }
            foreach (var v in addAssemblyAttributes)
            {
                module.Assembly.CustomAttributes.Add(v);
            }

        }
    }
}
