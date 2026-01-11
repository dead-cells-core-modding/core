using HaxeDocs;

namespace HashlinkNET.Compiler
{
    public class CompileConfig
    {
        public bool AllowParalle
        {
            get; set;
        }
        public bool GeneratePseudocode
        {
            get; set;
        }
        public bool GenerateBytecodeMapping
        {
            get; set;
        }
        public HaxeDocument? HaxeDocument
        {
            get; set;
        }
    }
}
