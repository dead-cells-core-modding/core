using Hashlink.Reflection.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Reflection.Members.Virtual
{
    public unsafe class HashlinkVirtualField(HashlinkModule module, HL_obj_field* ptr) : HashlinkField(module, ptr),
        IHashlinkMemberGenerator
    {
        private string? cachedName;
        private HashlinkType? cachedFieldType;

        public override HashlinkType FieldType => cachedFieldType ??= GetMemberFrom<HashlinkType>(ptr->t);
        public override int HashedName => ptr->hashed_name;
        public override string Name => cachedName ??= new(ptr->name);

        static HashlinkMember IHashlinkMemberGenerator.GenerateFromPointer( HashlinkModule module, void* ptr )
        {
            return new HashlinkVirtualField(module, (HL_obj_field*)ptr);
        }
    }
}
