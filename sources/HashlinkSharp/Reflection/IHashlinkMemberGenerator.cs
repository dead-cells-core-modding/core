using Hashlink.Reflection.Members;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Reflection
{
    public unsafe interface IHashlinkMemberGenerator
    {
        abstract static HashlinkMember GenerateFromPointer( HashlinkModule module, void* ptr );
    }
}
