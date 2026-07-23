using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Text;

namespace HashlinkNET.Compiler.Data.Interfaces
{
    internal interface IAltTypeReferenceValue
    {
        public TypeReference? AltTypeReference
        {
            get;
        }
    }
}
