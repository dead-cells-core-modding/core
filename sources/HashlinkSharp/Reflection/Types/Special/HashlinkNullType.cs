using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Reflection.Types.Special
{
    public unsafe class HashlinkNullType(HashlinkModule module, HL_type* type) : HashlinkSpecialType<HL_type>(module, type)
    {
        private HashlinkType? cachedRefType;

        public HashlinkType ValueType => cachedRefType ??= GetMemberFrom<HashlinkType>(TypeData);
    }
}
