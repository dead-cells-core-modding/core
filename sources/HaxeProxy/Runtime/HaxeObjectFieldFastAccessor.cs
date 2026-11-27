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
    /// <summary>
    /// Provides fast, cached access to a field of a Haxe object.
    /// </summary>
    /// <typeparam name="T">The Haxe object type that declares the field.</typeparam>
    public unsafe class HaxeObjectFieldFastAccessor<T> where T : HaxeObject, new()
    {
        private ObjFieldInfoCache fieldInfo;

        /// <summary>
        /// Initializes a new accessor for the specified field.
        /// </summary>
        /// <param name="fieldName">The name of the field to access.</param>
        public HaxeObjectFieldFastAccessor(string fieldName)
        {
            var obj = new T();
            HaxeProxyHelper.GetValueFieldById<byte>(obj, fieldName, ref fieldInfo);
        }

        /// <summary>
        /// Reads the field value from the specified object.
        /// </summary>
        /// <typeparam name="TValue">The unmanaged field value type.</typeparam>
        /// <param name="ptr">A pointer to the target Haxe object.</param>
        /// <returns>The field value.</returns>
        public TValue GetValue<TValue>( HashlinkObjPtr ptr ) where TValue : unmanaged, allows ref struct
        {
            Debug.Assert(fieldInfo.offset != 0);
            return *(TValue*)(ptr.Pointer + fieldInfo.offset);
        }

        /// <summary>
        /// Writes the field value to the specified object.
        /// </summary>
        /// <typeparam name="TValue">The unmanaged field value type.</typeparam>
        /// <param name="ptr">A pointer to the target Haxe object.</param>
        /// <param name="val">The value to write.</param>
        public void SetValue<TValue>( HashlinkObjPtr ptr, TValue val ) where TValue : unmanaged, allows ref struct
        {
            Debug.Assert(fieldInfo.offset != 0);
            *(TValue*)(ptr.Pointer + fieldInfo.offset) = val;
        }
    }
}
