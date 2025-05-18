using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Steps.Preprocessor.Types
{
    internal class GenerateVirtualTypeStep : GenerateTypeCompileStep
    {
        public override bool Filter( HlType type ) => type.Kind == HlTypeKind.Virtual;
        protected override bool SupportParalle => true;
        public override void Execute( IDataContainer data, HlCode code, GlobalData gdata, RuntimeImports rdata, HlType t )
        {
            if (t is not HlTypeWithVirtual vtype)
            {
                return;
            }
            var virt = vtype.Virtual;
            if (virt.Fields.Length == 0)
            {
                data.AddData(t, new VirtualClassData()
                {
                    ShortName = "virtual_",
                    FullName = "virtual_",
                    TypeRef = rdata.virtualType,
                    SortedFields = []
                });
                return;
            }
            var sortedFields = virt.Fields.ToList();
            sortedFields.Sort(( a, b ) => a.Name.CompareTo(b.Name));

            var sb = new StringBuilder();
            sb.Append("virtual_");
            foreach (var name in sortedFields)
            {
                sb.Append(name.Name);
                sb.Append('_');
            }

            var vname = sb.ToString();

            foreach (var v in sortedFields)
            {
                sb.Append(v.Type.Value.ToString());
                sb.Append('_');
            }

            var fullname = sb.ToString();

            var td = new TypeDefinition("Hashlink.Virtuals", vname.ToString(), TypeAttributes.Class |
            TypeAttributes.Public | TypeAttributes.Sealed)
            {
                BaseType = rdata.virtualType
            };

            addedTypes.Add(new(td, -1));

            addAssemblyAttributes.Add(new(
                        rdata.attrTypeBindingCtor
                        )
            {
                ConstructorArguments =
                        {
                            new(gdata.Module.TypeSystem.Int32, t.TypeIndex),
                            new(gdata.Module.TypeSystem.TypedReference, td)
                        }
            });

            data.AddData(td, t, new VirtualClassData()
            {
                TypeDef = td,
                TypeRef = td,
                FullName = fullname,
                ShortName = vname,
                SortedFields = sortedFields
            });

        }
    }
}
