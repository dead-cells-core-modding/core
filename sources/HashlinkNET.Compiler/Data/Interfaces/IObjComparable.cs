using Mono.Cecil;

namespace HashlinkNET.Compiler.Data.Interfaces
{
    interface IObjComparable
    {
        public MethodReference? Compare {
            get;
        }
    }
}
