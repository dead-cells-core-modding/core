using Hashlink.Reflection;
using Hashlink.Reflection.Members;
using HaxeProxy.Runtime;
using Mono.Cecil;
using System.Reflection;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace Haxe2CSharp
{
    internal class HaxeCompiler(
        string moduleName,
        HashlinkModule module,
        Assembly proxy )
    {
        private RuntimeHelperRef? runtimeHelperRef = null;

        public AssemblyDefinition Assembly {
            get;
        } = AssemblyDefinition.CreateAssembly(new(moduleName, new()), moduleName, ModuleKind.Dll);

        public ModuleDefinition MainModule => Assembly.MainModule;

        public void Compile( HashlinkFunction func )
        {
            runtimeHelperRef ??= new(MainModule);

            var dt = func.DeclaringType == null ? null : HaxeProxyUtils.GetProxyType(func.DeclaringType);

            var dtNspec = dt?.Namespace;
            var dtName = dt?.Name ?? "UnknownType";

            var dtd = MainModule.GetType(dtNspec, dtName);

            if (dtd == null)
            {
                dtd = new TypeDefinition(dtNspec, dtName, TypeAttributes.NotPublic | TypeAttributes.Sealed);
                MainModule.Types.Add(dtd);
            }


        }
    }
}
