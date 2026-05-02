using Hashlink.Reflection;
using Hashlink.Reflection.Members;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Haxe2CSharp
{
    internal class HaxeCompiler(
        string moduleName,
        HashlinkModule module,
        Assembly proxy)
    {
        public AssemblyDefinition Assembly
        {
            get;
        } = AssemblyDefinition.CreateAssembly(new(moduleName, new()), moduleName, ModuleKind.Dll);

        public void Compile(HashlinkFunction func)
        {
        
        }
    }
}
