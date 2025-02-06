using Hashlink.Reflection.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Reflection.Members.Enum
{
    public unsafe class HashlinkEnumConstruct( HashlinkModule module, HL_enum_construct* ptr ) : HashlinkMember(module, ptr),
        IHashlinkMemberGenerator
    {
        private HashlinkType[]? cachedParamTypes;
        private int[]? cachedOffsets;
        private string? cachedName;
        static HashlinkMember IHashlinkMemberGenerator.GenerateFromPointer( HashlinkModule module, void* ptr )
        {
            return new HashlinkEnumConstruct(module, (HL_enum_construct*)ptr);
        }

        public override string? Name => cachedName ??= new(ptr->name);
        public int ParamsCount => ptr->nparams;
        public int[] ParamOffsets
        {
            get
            {
                if (cachedOffsets == null)
                {
                    cachedOffsets = new int[ParamsCount];
                    for (int i = 0; i < ParamsCount; i++)
                    {
                        cachedOffsets[i] = ptr->offsets[i];
                    }
                }
                return cachedOffsets;
            }
        }
        public HashlinkType[] Params
        {
            get
            {
                if (cachedParamTypes == null)
                {
                    cachedParamTypes = new HashlinkType[ParamsCount];
                    for (int i = 0; i < ParamsCount; i++)
                    {
                        cachedParamTypes[i] = GetMemberFrom<HashlinkType>(ptr->@params[i]);
                    }
                }
                return cachedParamTypes;
            }
        }
    }
}
