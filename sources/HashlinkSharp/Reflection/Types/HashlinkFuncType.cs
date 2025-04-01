using Hashlink.Proxy.Clousre;
using Hashlink.Reflection.Members;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Reflection.Types
{
    public unsafe class HashlinkFuncType(HashlinkModule module, HL_type* type) : HashlinkSpecialType<HL_type_func>(module, type)
    {
        private HashlinkType? cachedReturnType;
        private HashlinkType[]? cachedArgTypes;
        private HashlinkFuncType? cachedRealFunc;

        public bool IsClosureType => TypeData->parent != null;
        public HashlinkFuncType BaseFunc => cachedRealFunc ??= !IsClosureType ? this : GetMemberFrom<HashlinkFuncType>(TypeData->parent);

        public HashlinkType ReturnType => cachedReturnType ??= GetMemberFrom<HashlinkType>(TypeData->ret);
        public HashlinkType[] ArgTypes
        {
            get
            {
                if (cachedArgTypes == null)
                {
                    cachedArgTypes = new HashlinkType[TypeData->nargs];
                    for (int i = 0; i < TypeData->nargs; i++)
                    {
                        cachedArgTypes[i] = GetMemberFrom<HashlinkType>(TypeData->args[i]);
                    }
                }
                return cachedArgTypes;
            }
        }

        public HashlinkClosure CreateClosure( void* entry )
        {
            return new HashlinkClosure(NativeType, entry, null);
        }
    }
}
