using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink
{
    public unsafe interface IHashlinkCustomMarshaler
    {
        public bool TryWriteData(void* target, HL_type.TypeKind? typeKind);
    }
}
