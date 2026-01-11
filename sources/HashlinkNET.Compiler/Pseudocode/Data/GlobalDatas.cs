using BytecodeMapping;
using HashlinkNET.Bytecode;
using Mono.Cecil;

namespace HashlinkNET.Compiler.Pseudocode.Data
{
    record class FuncEmitGlobalData(
        HlFunction Function,
        HlTypeFun FuncType,
        MethodDefinition Definition,
        BytecodeMappingData.FunctionData MappingData
        )
    {

        public List<HlBasicBlockData> HlBasicBlocks
        {
            get;
        } = [];
        public List<IRBasicBlockData> IRBasicBlocks
        {
            get;
        } = [];
        public Dictionary<int, List<string>> Assigns
        {
            get;
        } = [];
        public int VariablesCount
        {
            get; set;
        }

        public List<HlFuncRegisterData> LocalRegisters
        {
            get; set;
        } = [];
        public List<HlFuncRegisterData?> Registers
        {
            get;
        } = [];
    }
}
