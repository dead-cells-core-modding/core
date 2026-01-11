using HashlinkNET.Compiler.Data;
using HaxeProxy.Codegen;
using HaxeProxy.Runtime.Internals;
using Mono.Cecil;
using System.Reflection;

namespace HashlinkNET.Compiler.Steps
{
    internal class ApplyBuiltinModifierStep : ParallelCompileStep<TypeDefinition>
    {
        private readonly List<BuiltinModifier> modifiers = [];
        protected override void Initialize( IDataContainer container )
        {
            base.Initialize(container);

            {
                Type?[] types;
                try
                {
                    types = typeof(HaxeProxyHelper).Assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types;
                }
                foreach (var v in types)
                {
                    if (v is null)
                    {
                        continue;
                    }
                    if (v.IsSubclassOf(typeof(BuiltinModifier)))
                    {
                        modifiers.Add((BuiltinModifier)Activator.CreateInstance(v)!);
                    }
                }
            }
        }

        protected override void Execute( IDataContainer container, TypeDefinition item, int index )
        {
            foreach (var v in modifiers)
            {
                if (!v.CanModify(item))
                {
                    continue;
                }
                v.Modify(item);
            }
        }

        protected override IReadOnlyList<TypeDefinition> GetItems( IDataContainer container )
        {
            return [..container.GetGlobalData<GlobalData>().Assembly.MainModule.Types];
        }
    }
}
