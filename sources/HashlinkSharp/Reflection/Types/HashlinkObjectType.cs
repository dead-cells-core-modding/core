using Hashlink.Proxy;
using Hashlink.Proxy.Objects;
using Hashlink.Reflection.Members;
using Hashlink.Reflection.Members.Object;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Reflection.Types
{
    public unsafe class HashlinkObjectType(HashlinkModule module, HL_type* type) : 
        HashlinkSpecialType<HL_type_obj>(module, type)
    {
        private HashlinkObjectType? cachedSuper;
        private string? cachedName;
        private HashlinkField[]? cachedFields;
        private HashlinkObjectProto[]? cachedProtos;
        public HashlinkObjectType? Super => cachedSuper ??= GetMemberFrom<HashlinkObjectType>(TypeData->super);
        public override string Name => cachedName ??= new(TypeData->name);
        public override HashlinkObj CreateInstance()
        {
            return new HashlinkObject(NativeType);
        }

        public HashlinkObjectProto[] Protos
        {
            get
            {
                if (cachedProtos == null)
                {
                    cachedProtos = new HashlinkObjectProto[TypeData->nproto];
                    for (int i = 0; i < TypeData->nproto; i++)
                    {
                        cachedProtos[i] = GetMemberFrom<HashlinkObjectProto>(TypeData->proto + i);
                    }
                }
                return cachedProtos;
            }
        }
        public HashlinkField[] Fields
        {
            get
            {
                if (cachedFields == null)
                {
                    cachedFields = new HashlinkField[TypeData->nfields];
                    for (int i = 0; i < TypeData->nfields; i++)
                    {
                        cachedFields[i] = GetMemberFrom<HashlinkObjectField>(TypeData->fields + i);
                    }
                }
                return cachedFields;
            }
        }
    }
}
