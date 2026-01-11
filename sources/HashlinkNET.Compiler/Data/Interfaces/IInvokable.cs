using Mono.Cecil;

namespace HashlinkNET.Compiler.Data.Interfaces
{
    interface IInvokable
    {
        public MethodReference Invoke
        {
            get;
        }
    }
}
