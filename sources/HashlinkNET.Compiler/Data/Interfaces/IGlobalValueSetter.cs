using HashlinkNET.Bytecode;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Data.Interfaces
{
    interface IGlobalValueSetter
    {
        TypeDefinition? GlobalClassType
        {
            get; set;
        }
        PropertyDefinition? GlobalClassProp
        {
            get; set;
        }
        FieldDefinition? GlobalClassField
        {
            get; set;
        }
        HlTypeWithObj? GlobalHlType
        {
            get; set;
        }
    }
}
