using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using Mono.Cecil;

namespace HashlinkNET.Compiler.Steps.Preprocessor.Types.Virtuals
{
    internal class GenerateVirtualTypeStep : GenerateTypeCompileStep
    {
        public override bool Filter( HlType type ) => type.Kind == HlTypeKind.Virtual;
        protected override bool SupportParalle => true;
        public override void Execute( IDataContainer data, HlCode code, GlobalData gdata,
            RuntimeImports rdata, HlType t )
        {
            if (t is not HlTypeWithVirtual vtype)
            {
                return;
            }
            var virt = vtype.Virtual;
            var group = data.GetData<VirtualGroupData>(vtype);
            if (virt.Fields.Length == 0)
            {
                data.AddData(t, new VirtualClassData()
                {
                    TypeRef = rdata.virtualType,
                    Group = group,
                });
                return;
            }

            TypeReference tr;

            if (group.Types.Count == 1 ||
                group.DifferentTypeFields.Count == 0)
            {
                tr = group.TypeDef;
            }
            else
            {
                var git = new GenericInstanceType(group.TypeDef);
                tr = git;
            }

            addedTypes.Add(new(tr, AddTypeKind.AddToTypesList, t.TypeIndex));

            //addAssemblyAttributes.Add(new(
            //            rdata.attrTypeBindingCtor
            //            )
            //{
            //    ConstructorArguments =
            //            {
            //                new(gdata.Module.TypeSystem.Int32, t.TypeIndex),
            //                new(gdata.Module.TypeSystem.TypedReference, tr)
            //            }
            //});

            data.AddData(tr, t, new VirtualClassData()
            {
                TypeRef = tr,
                Group = group
            });

        }
    }
}
