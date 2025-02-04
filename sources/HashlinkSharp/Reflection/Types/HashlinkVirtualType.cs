using Hashlink.Reflection.Members.Object;
using Hashlink.Reflection.Members.Virtual;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Reflection.Types
{
    public unsafe class HashlinkVirtualType(HashlinkModule module, HL_type* type) : 
        HashlinkSpecialType<HL_type_virtual>(module, type)
    {
        private HashlinkVirtualField[]? cachedFields;

        public HashlinkVirtualField[] Fields
        {
            get
            {
                if (cachedFields == null)
                {
                    cachedFields = new HashlinkVirtualField[TypeData->nfields];
                    for (int i = 0; i < TypeData->nfields; i++)
                    {
                        cachedFields[i] = GetMemberFrom<HashlinkVirtualField>(TypeData->fields + i);
                    }
                }
                return cachedFields;
            }
        }
    }
}
