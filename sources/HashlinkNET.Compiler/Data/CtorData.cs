using HashlinkNET.Compiler.Data.Interfaces;
using Mono.Cecil;

namespace HashlinkNET.Compiler.Data
{
    /// <summary>
    /// Registers an explicit constructor for a type that is not an
    /// <see cref="ObjClassData"/> (for example an enum item type).
    /// </summary>
    internal class CtorData( MethodReference construct ) : IConstructable
    {
        public MethodReference Construct {
            get;
        } = construct;
    }
}
