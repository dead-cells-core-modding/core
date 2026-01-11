using Mono.Cecil;

namespace HashlinkNET.Compiler.Data.Interfaces
{
    interface IGetProto
    {
        public MethodReference? GetProto( int index );
    }
}
