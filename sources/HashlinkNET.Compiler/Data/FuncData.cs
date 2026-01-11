using Mono.Cecil;

namespace HashlinkNET.Compiler.Data
{
    internal class FuncData
    {
        public ObjClassData? DeclaringClass
        {
            get; set;
        }
        public required MethodDefinition Definition
        {
            get; set;
        }
        public List<(FuncData, int)> UsedBy
        {
            get; set;
        } = [];
    }
}
