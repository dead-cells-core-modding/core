using Hashlink.Marshaling;
using Hashlink.Marshaling.ObjHandle;
using Hashlink.Proxy.DynamicAccess;
using Hashlink.Reflection.Types;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq.Expressions;

namespace Hashlink.Proxy
{
    public abstract unsafe partial class HashlinkObj : 
        IHashlinkPointer
        //,IDynamicMetaObjectProvider
    {
        [MemberNotNull(nameof(nativeType))]
        [MemberNotNull(nameof(type))]
        internal void RefreshTypeInfo( HL_type* ptr, bool clearExtraData )
        {
            nativeType = ptr;
            type = HashlinkMarshal.Module.GetMemberFrom<HashlinkType>(nativeType);
            isChangedTypeInfo = true;
            if (clearExtraData)
            {
                ClearExtraData();
            }
        }

        internal bool isChangedTypeInfo = false;
      
        public HashlinkObj( HashlinkObjPtr objPtr )
        {
            var ptr = objPtr.Pointer;
            Handle = HashlinkObjManager.GetHandle(ptr);
            if (Handle != null)
            {
                Handle.Target = this;
            }

            HashlinkPointer = ptr;
            nativeType = *(HL_type**)ptr;
            type = HashlinkMarshal.Module.GetMemberFrom<HashlinkType>(nativeType);
        }
        public override string ToString()
        {
            return new string(hl_to_string((HL_vdynamic*)HashlinkPointer)) ?? Type.Name ?? "";
        }

        public void MarkStateful()
        {
            if (Handle != null)
            {
                Handle.IsStateless = false;
            }
        }

        public DynamicMetaObject GetMetaObject( Expression parameter )
        {
            dynamicAccess ??= (HashlinkObjDynamicAccess) HashlinkObjDynamicAccess.Create(this);
            return dynamicAccess.GetMetaObject(parameter);
        }

        public HashlinkObjHandle? Handle
        {
            get; 
        }
        public TypeKind TypeKind => Type.TypeKind;

        private HL_type* nativeType;
        private HashlinkType? type;

        private HashlinkObjDynamicAccess? dynamicAccess;
        public HashlinkType Type => type!;
        public HL_type* NativeType => nativeType;
        public virtual nint HashlinkPointer
        {
            get; 
        }
    }
}
