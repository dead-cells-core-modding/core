using Hashlink.Marshaling;
using Hashlink.Reflection.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Reflection.Members.Object
{
    public unsafe class HashlinkObjectBinding : HashlinkMember
    {
        public HashlinkObjectBinding(HashlinkModule module, int* ptr,  HashlinkObjectType type) : base(module, ptr)
        {
            Field = type.FindFieldById(ptr[0])!;
            Function = (HashlinkFunction)module.GetFunctionByFIndex(ptr[1]);
        }

        public HashlinkField Field
        {
            get;
        }
        public HashlinkFunction Function
        {
            get;
        }
        public override string? Name => Field.Name;
        public override int HashedName => Field.HashedName;

    }
}
