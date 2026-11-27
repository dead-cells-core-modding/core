using Hashlink.Proxy;
using HaxeProxy.Runtime.Internals;
using HaxeProxy.Runtime.Internals.Cache;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace HaxeProxy.Runtime
{
    public unsafe class HaxeObjectFieldFastAccessor<T> where T : HaxeObject, new()
    {
        private ObjFieldInfoCache fieldInfo;

        public HaxeObjectFieldFastAccessor(string fieldName)
        {
            var obj = new T();
            HaxeProxyHelper.GetValueFieldById<byte>(obj, fieldName, ref fieldInfo);
        }

        public TValue GetValue<TValue>( HashlinkObjPtr ptr ) where TValue : unmanaged, allows ref struct
        {
            Debug.Assert(fieldInfo.offset != 0);
            return *(TValue*)(ptr.Pointer + fieldInfo.offset);
        }

        public void SetValue<TValue>( HashlinkObjPtr ptr, TValue val ) where TValue : unmanaged, allows ref struct
        {
            Debug.Assert(fieldInfo.offset != 0);
            *(TValue*)(ptr.Pointer + fieldInfo.offset) = val;
        }
    }
}
