using Hashlink.Proxy.Clousre;
using Hashlink.Proxy.Objects;
using Hashlink.Reflection.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Reflection.Members.Object
{
    public unsafe class HashlinkObjectProto( HashlinkModule module, HL_obj_proto* proto ) : HashlinkMember(module, proto),
        IHashlinkMemberGenerator
    {
        private string? cachedName;
        private HashlinkFunction? cachedFunction;

        public override string? Name => cachedName ??= new(proto->name);
        public HashlinkFunction Function => cachedFunction ??= Module.GetFunctionByFIndex(FunctionIndex);
        public int FunctionIndex => proto->findex;
        public int ProtoIndex => proto->pindex;
        public bool IsVirtual => ProtoIndex >= 0;

        static HashlinkMember IHashlinkMemberGenerator.GenerateFromPointer( HashlinkModule module, void* ptr )
        {
            return new HashlinkObjectProto(module, (HL_obj_proto*)ptr);
        }
       
    }
}
