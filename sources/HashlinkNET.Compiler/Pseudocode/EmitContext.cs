using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Pseudocode.Data;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode
{
    record class EmitContext(
        IDataContainer DataContainer, 
        MethodDefinition Definition,
        ModuleDefinition Module,
        TypeSystem TypeSystem,
        RuntimeImports RuntimeImports,
        CompileConfig Config,
        ScopeDebugInformation Scope,
        ILProcessor IL,
        FuncEmitGlobalData GlobalData)
    {
        public ScopeDebugInformation?[] VariableDebugs
        {
            get; set;
        } = [];
        public bool RequestValue
        {
            get; set;
        }
    }
}
