using Hashlink.Reflection;
using Hashlink.Reflection.Members;
using HashlinkNET.Bytecode;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Text;

namespace Haxe2CSharp
{
    internal record class CompilerContext(
        HaxeCompiler Compiler, 
        RuntimeHelperRef RHR, 
        ModuleDefinition Module,
        MethodDefinition Method,
        ILProcessor IL,
        HashlinkFunction Function,
        HashlinkModule HashlinkModule,
        HlCode Code,
        HlFunction FuncCode)
    {
        public TypeSystem TS => Module.TypeSystem;
    }
}
