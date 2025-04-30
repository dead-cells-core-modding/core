using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Data.Interfaces
{
    interface IGetProto
    {
        public MethodReference? GetProto( int index );
    }
}
