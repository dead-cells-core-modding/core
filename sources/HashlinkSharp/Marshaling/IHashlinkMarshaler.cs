using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Marshaling
{
    public unsafe interface IHashlinkMarshaler
    {
        object? TryReadData(void* target, HL_type.TypeKind? typeKind);
        bool TryWriteData(void* target, object? value, HL_type.TypeKind? typeKind);
    }
}
