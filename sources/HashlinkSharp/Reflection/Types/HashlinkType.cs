using Hashlink.Marshaling;
using Hashlink.Proxy;
using Hashlink.Reflection.Members;
using Hashlink.Reflection.Types.Special;

namespace Hashlink.Reflection.Types
{
    public unsafe class HashlinkType( HashlinkModule module, HL_type* type ) : HashlinkMember(module, type),
        IHashlinkMemberGenerator
    {
        private string? cachedName;
        public HL_type* NativeType {
            get;
        } = type;

        private static HashlinkObjectType ParseObjType( HashlinkModule module, HL_type* type )
        {
            var name = new string(type->data.obj->name);
            if (name == "String")
            {
                return new HashlinkStringType(module, type);
            }
            return new HashlinkObjectType(module, type);
        }
        static HashlinkMember IHashlinkMemberGenerator.GenerateFromPointer( HashlinkModule module, void* ptr )
        {
            var type = (HL_type*)ptr;
            var kind = type->kind;
            if (kind < TypeKind.HBYTES)
            {
                return new HashlinkType(module, type);
            }
            return kind switch
            {
                TypeKind.HOBJ => ParseObjType(module, type),
                TypeKind.HFUN => new HashlinkFuncType(module, type),
                TypeKind.HMETHOD => new HashlinkFuncType(module, type),
                TypeKind.HARRAY => new HashlinkArrayType(module, type),
                TypeKind.HVIRTUAL => new HashlinkVirtualType(module, type),
                TypeKind.HABSTRACT => new HashlinkAbstractType(module, type),
                TypeKind.HREF => new HashlinkRefType(module, type),
                TypeKind.HNULL => new HashlinkNullType(module, type),
                TypeKind.HENUM => new HashlinkEnumType(module, type),
                _ => new HashlinkType(module, type)
            };
        }
        public virtual HashlinkObj CreateInstance()
        {
            throw new NotImplementedException();
        }
        public override string? Name => cachedName ??= NativeType->TypeName;
        public TypeKind TypeKind => NativeType->kind;
        public virtual int SizeOf => hl_type_size(NativeType);

        public int TypeIndex {
            get;
            internal set;
        } = -1;
    }
}
