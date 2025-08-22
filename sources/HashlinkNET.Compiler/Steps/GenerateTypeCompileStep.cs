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
        [Flags]
        protected enum AddTypeKind
        {
            None,
            AddToModule = 1,
            AddToTypesList = 2
        }
        protected record struct AddTypeInfo(
            TypeReference Type,
            AddTypeKind Kind,
            int Index = 0
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
                if (v.Kind.HasFlag(AddTypeKind.AddToModule) && v.Type is TypeDefinition td )
                {
                    module.Types.Add(td);
                }
                if (v.Kind.HasFlag(AddTypeKind.AddToTypesList))
                {
                    if (v.Type is TypeDefinition t)
                    {
                        t.CustomAttributes.Add(new(
                            rdata.attrTIndexCtor)
                        {
                            ConstructorArguments =
                            {
                                new(gdata.Module.TypeSystem.Int32, v.Index)
                            }
                        });
                    }
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
