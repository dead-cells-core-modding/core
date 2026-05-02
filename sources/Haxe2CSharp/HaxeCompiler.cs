using Hashlink.Reflection;
using Hashlink.Reflection.Members;
using HashlinkNET.Bytecode;
using HaxeProxy.Runtime;
using Mono.Cecil;
using System.Diagnostics;
using System.Reflection;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace Haxe2CSharp
{
    internal class HaxeCompiler(
        string moduleName,
        HashlinkModule module,
        Assembly proxy,
        HlCode code)
    {
        private RuntimeHelperRef? runtimeHelperRef = null;

        public AssemblyDefinition Assembly {
            get;
        } = AssemblyDefinition.CreateAssembly(new(moduleName, new()), moduleName, ModuleKind.Dll);

        public ModuleDefinition MainModule => Assembly.MainModule;


        public void Compile( HashlinkFunction func )
        {
            runtimeHelperRef ??= new(MainModule);

            var mm = MainModule;

            var dt = func.DeclaringType == null ? null : HaxeProxyUtils.GetProxyType(func.DeclaringType);

            var dtNspec = dt?.Namespace;
            var dtName = dt?.Name ?? "UnknownType";

            var dtd = MainModule.GetType(dtNspec, dtName);

            if (dtd == null)
            {
                dtd = new TypeDefinition(dtNspec, dtName, TypeAttributes.NotPublic | TypeAttributes.Sealed);
                MainModule.Types.Add(dtd);
            }

            var mdt = HaxeProxyUtils.GetProxyType(func.FuncType).GetMethod("Invoke");

            Debug.Assert(mdt != null);

            var md = new MethodDefinition($"{(func.Name ?? "func")}_{func.FunctionIndex}", Mono.Cecil.MethodAttributes.Static |
                Mono.Cecil.MethodAttributes.Public, mm.ImportReference(mdt.ReturnType))
            {
                CustomAttributes =
                {
                    new(runtimeHelperRef.attrFIndexCtor)
                    {
                        ConstructorArguments =
                        {
                            new(MainModule.TypeSystem.Int32, func.FunctionIndex)
                        }
                    }
                }
            };

            dtd.Methods.Add(md);

            foreach (var p in mdt.GetParameters())
            {
                md.Parameters.Add(new(p.Name, Mono.Cecil.ParameterAttributes.None, mm.ImportReference(p.ParameterType)));
            }

            var ilp = md.Body.GetILProcessor();

            var funcCode = code.GetFunctionById(func.FunctionIndex);

            Debug.Assert(funcCode != null);

            var ctx = new CompilerContext(this, runtimeHelperRef, mm, md, ilp, func, module, code, funcCode);

            var fc = new FuncCompiler2(ctx);
            fc.Compile();

        }
    }
}
