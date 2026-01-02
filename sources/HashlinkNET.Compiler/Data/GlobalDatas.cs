using BytecodeMapping;
using HashlinkNET.Bytecode;
using HaxeProxy.Codegen;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

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
        List<BuiltinModifier> Modifiers,
        HlCode Code,
        BytecodeMappingData BytecodeMappingData,
        XDocument XmlDocument
    );
    internal record class VirtualTypeList
    (
        Dictionary<string, VirtualGroupData> Virtuals
    );
    internal record class NativeImplClasses
    (
        Dictionary<string, TypeDefinition> NativeImplClass
    );
}
