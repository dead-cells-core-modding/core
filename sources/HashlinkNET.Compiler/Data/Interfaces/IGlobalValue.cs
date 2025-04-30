using HashlinkNET.Bytecode;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Data.Interfaces
{
    interface IGlobalValue
    {
        TypeDefinition? GlobalClassType
        {
            get;
        }
        PropertyDefinition? GlobalClassProp
        {
            get;
        }
        FieldDefinition? GlobalClassField
        {
            get;
        }
        HlTypeWithObj? GlobalHlType
        {
            get; 
        }
    }
}
