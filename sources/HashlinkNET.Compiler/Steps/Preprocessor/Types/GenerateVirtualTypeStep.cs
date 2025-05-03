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
        private readonly ConcurrentDictionary<string, TypeDefinition> virtualClasses = [];
        
        public override bool Filter( HlType type ) => type.Kind == HlTypeKind.Virtual;
        protected override bool SupportParalle => false;
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
                    TypeRef = rdata.virtualType,
                    SortedFields = []
                });
                return;
            }
            var sortedFields = virt.Fields.ToList();
            sortedFields.Sort((a, b) => a.Name.CompareTo(b.Name));

            var sb = new StringBuilder();
            sb.Append("virtual_");
            foreach (var name in sortedFields)
            {
                sb.Append(name.Name);
                sb.Append('_');
            }



            var vname = sb.ToString();

            var virtType = virtualClasses.GetOrAdd(vname, _ =>
            {
                var td = new TypeDefinition("Hashlink.Virtuals", sb.ToString(), TypeAttributes.Class |
                TypeAttributes.Public | TypeAttributes.Sealed)
                {
                    BaseType = rdata.virtualType
                };

                foreach (var v in sortedFields)
                {
                    var gp = new GenericParameter("T" + v.Name, td);
                    td.GenericParameters.Add(gp);
                    var fd = new PropertyDefinition(v.Name, PropertyAttributes.None, gp);
                    td.EmitFieldGetterSetter(fd, data, v.Name);
                    td.Properties.Add(fd);
                }

                addedTypes.Add(new(td, -1));
                return td;
            });

            var gt = new GenericInstanceType(virtType);

            addAssemblyAttributes.Add(new(
                        rdata.attrTypeBindingCtor
                        )
            {
                ConstructorArguments =
                        {
                            new(gdata.Module.TypeSystem.Int32, t.TypeIndex),
                            new(gdata.Module.TypeSystem.TypedReference, gt)
                        }
            });
            
            data.AddData(gt, t, new VirtualClassData()
            {
                TypeRef = gt,
                SortedFields = sortedFields
            });

        }
    }
}
