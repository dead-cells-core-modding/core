using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HashlinkNET.Compiler.Steps.Preprocessor.Types.Virtuals
{
    internal class ScanVirtualTypeStep : ForeachHlTypeCompileStep
    {
        private VirtualTypeList typeList = null!;
        protected override bool SupportParalle => false;
        public override bool Filter( HlType type )
        {
            return type.Kind == HlTypeKind.Virtual;
        }
        protected override void Initialize( IDataContainer container )
        {
            base.Initialize(container);
            typeList = container.AddGlobalData<VirtualTypeList>(
                new(
                    []
                  )
                );
        }
        protected override void PostProcessing( IDataContainer container )
        {
            base.PostProcessing(container);

            foreach ((_, var group) in typeList.Virtuals)
            {
                var ftl = new Dictionary<string, HlType>();
                var td = group.TypeDef;
                foreach (var t in group.Types)
                {
                    foreach (var f in t.Virtual.Fields)
                    {
                        if (string.IsNullOrEmpty(f.Name))
                        {
                            continue;
                        }
                        if (ftl.TryGetValue(f.Name, out var ftlType))
                        {
                            if (ftlType != f.Type.Value)
                            {
                                group.DifferentTypeFields.Add(f.Name);
                            }
                        }
                        else
                        {
                            ftl[f.Name] = f.Type.Value;
                        }
                    }
                }

                if (group.DifferentTypeFields.Count != 0)
                {
                    for (int i = 0; i < group.SortedFieldNames.Count; i++)
                    {
                        var fn = group.SortedFieldNames[i];
                        if (group.DifferentTypeFields.Contains(fn))
                        {
                            td.GenericParameters.Add(new(fn, td));
                        }
                    }
                }
            }
        }
        public override void Execute( IDataContainer container, HlCode code, 
            GlobalData gdata, RuntimeImports rdata, HlType type )
        {

            var virt = ((HlTypeWithVirtual)type).Virtual;

            var sortedFields = virt.Fields.ToList();
            sortedFields.Sort(( a, b ) => a.Name.CompareTo(b.Name));

            var sb = new StringBuilder();
            sb.Append("virtual_");
            foreach (var name in sortedFields)
            {
                sb.Append(name.Name);
                sb.Append('_');
            }
            var shortname = sb.ToString();

            if (!typeList.Virtuals.TryGetValue(shortname, out var group))
            {
                group = new()
                {
                    Name = shortname,
                    TypeDef = new("Hashlink.Virtuals", shortname, TypeAttributes.Class |
                        TypeAttributes.Public | TypeAttributes.Sealed)
                    {
                        BaseType = rdata.virtualType
                    },
                    SortedFieldNames = [.. sortedFields.Select(x => x.Name)]
                };
                gdata.Module.Types.Add(group.TypeDef);
                typeList.Virtuals.Add(shortname, group);
            }
            group.Types.Add((HlTypeWithVirtual)type);

            container.AddData(type, group);
        }
    }
}
