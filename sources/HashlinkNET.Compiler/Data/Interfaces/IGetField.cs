using Mono.Cecil;

namespace HashlinkNET.Compiler.Data.Interfaces
{
    interface IGetField
    {
        public PropertyDefinition? GetField( int index );
    }
}
