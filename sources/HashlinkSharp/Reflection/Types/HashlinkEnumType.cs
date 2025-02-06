using Hashlink.Proxy;
using Hashlink.Proxy.Values;
using Hashlink.Reflection.Members.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Reflection.Types
{
    public unsafe class HashlinkEnumType(HashlinkModule module, HL_type* type) : 
        HashlinkSpecialType<HL_type_enum>(module, type)
    {
        private HashlinkEnumConstruct[]? cachedConstructs;
        public override HashlinkObj CreateInstance()
        {
            return new HashlinkEnum(this, 0);
        }
        
        public HashlinkEnumConstruct[] Constructs
        {
            get
            {
                if (cachedConstructs == null)
                {
                    cachedConstructs = new HashlinkEnumConstruct[TypeData->nconstructs];
                    for (int i = 0; i < TypeData->nconstructs; i++)
                    {
                        cachedConstructs[i] = GetMemberFrom<HashlinkEnumConstruct>(TypeData->constructs + i);
                    }
                }
                return cachedConstructs;
            }
        }
    }
}
