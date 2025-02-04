using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Reflection.Types
{
    public unsafe class HashlinkRefType(HashlinkModule module, HL_type* type) : HashlinkSpecialType<HL_type>(module, type)
    {
        private HashlinkType? cachedRefType;

        public HashlinkType RefType => cachedRefType ??= GetMemberFrom<HashlinkType>(TypeData);
    }
}
