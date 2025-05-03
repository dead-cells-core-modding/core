using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Steps.Virtual
{
    internal class FixVirtualFieldsStep : ForeachHlTypeCompileStep
    {
        private bool IsRefSelf( HlType type, HlType virt, HashSet<HlType>? types = null )
        {
            types ??= [];
            if (types.Contains(type))
                return true;
            types.Add(type);
            if (type is HlTypeWithFun fun)
            {
                return IsRefSelf(fun.FunctionDescription.ReturnType.Value, virt, types)
                    || fun.FunctionDescription.Arguments.Any(x => IsRefSelf(x.Value, virt, types));
            }
            else if (type is HlTypeWithVirtual vi)
            {
                return vi.Virtual.Fields.Any(x => IsRefSelf(x.Type.Value, virt, types));
            }
            return false;
        }
        public override bool Filter( HlType type )
        {
            return type.Kind == HlTypeKind.Virtual;
        }
        public override void Execute( IDataContainer container, HlCode code, GlobalData gdata,
            RuntimeImports rdata, HlType type )
        {
            var vi = container.GetData<VirtualClassData>(type);
            if (vi.TypeRef is not GenericInstanceType gt)
            {
                return;
            }
            
            var fields = vi.Fields;
            fields.AddRange(Enumerable.Repeat<PropertyDefinition>(null!, vi.SortedFields.Count));
            foreach (var f in vi.SortedFields)
            {
                TypeReference ft;
                if (!IsRefSelf(f.Type.Value, type))
                {
                    ft = container.GetTypeRef(f.Type.Value);
                }
                else
                {
                    ft = f.Type.Value.Kind switch
                    {
                        HlTypeKind.Virtual => rdata.virtualType,
                        HlTypeKind.Fun => rdata.delegateType,
                        _ => gdata.Module.TypeSystem.Object
                    }
                         ;
                }
                var fr = new PropertyDefinition(f.Name, PropertyAttributes.None, ft);
                fields[f.Index] = fr;
                gt.GenericArguments.Add(
                   ft
                );
            }
        }
    }
}
