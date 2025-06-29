using HashlinkNET.Bytecode;
using HashlinkNET.Bytecode.OpCodeParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Patch.Reader
{
    public abstract class HlOpCodeWriter
    {
        public abstract void MoveNext( HlOpcodeKind op );
        public abstract void Write( int val, HlOpCode.PayloadKind kind = HlOpCode.PayloadKind.None );
    }
}
