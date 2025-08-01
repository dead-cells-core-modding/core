using BytecodeMapping;
using HashlinkNET.Bytecode;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Data
{
    internal record class FunctionTypes( 
        TypeReference[] FuncTypes, 
        TypeReference[] ActionTypes 
        );
    internal record class GlobalData
    (
        CompileConfig Config,
        AssemblyDefinition Assembly,
        ModuleDefinition Module,
        HlCode Code,
        BytecodeMappingData BytecodeMappingData
    );
    internal record class NativeImplClasses
    (
        Dictionary<string, TypeDefinition> NativeImplClass
    );
}
