using Mono.Cecil;

namespace HashlinkNET.Compiler.Data.Interfaces
{
    interface IConstructable
    {
        public MethodReference Construct
        {
            get;
        }
    }
}
