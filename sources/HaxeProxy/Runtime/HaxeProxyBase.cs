using Hashlink;
using Hashlink.Marshaling;
using Hashlink.Proxy;
using Hashlink.Proxy.Objects;
using HaxeProxy.Runtime.Internals;
using System.Diagnostics;
using System.Dynamic;

namespace HaxeProxy.Runtime
{
    /// <summary>
    /// Base class for all managed proxies that wrap a Hashlink object.
    /// </summary>
    public abstract unsafe class HaxeProxyBase :
        DynamicObject,
        IExtraData,
        IExtraDataItem,
        IHashlinkPointer
    {
        /// <summary>
        /// Initializes a new proxy for the specified Hashlink object.
        /// </summary>
        /// <param name="obj">The Hashlink object to wrap.</param>
        protected HaxeProxyBase( HashlinkObj obj )
        {
            Debug.Assert(obj != null);
            HashlinkObj = obj;
            if (!createByManager)
            {
                IExtraData ied = obj;
                if (ied.GetOrCreateData<HaxeProxyBase>(_ => this) != this)
                {
                    throw new InvalidOperationException();
                }
                HaxeProxyManager.CheckCustomProxy(this, obj);
            }
            AfterBinding();
        }
        /// <summary>
        /// Gets the wrapped Hashlink object.
        /// </summary>
        public HashlinkObj HashlinkObj {
            get;
        }

        /// <summary>
        /// Gets the native pointer of the wrapped Hashlink object.
        /// </summary>
        public nint HashlinkPointer => ((IHashlinkPointer)HashlinkObj).HashlinkPointer;


        internal bool createByManager;

        static object IExtraDataItem.Create( HashlinkObj obj )
        {
            return HaxeProxyManager.CreateProxy(obj);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return HashlinkObj.ToString();
        }
        /// <summary>
        /// Called after the proxy has been bound to its Hashlink object.
        /// </summary>
        protected virtual void AfterBinding()
        {
        }

        /// <inheritdoc/>
        public override bool TryConvert( ConvertBinder binder, out object? result )
        {
            if (binder.Type.IsAssignableTo(typeof(HashlinkObj)))
            {
                result = HashlinkObj;
                return true;
            }
            if (binder.Type == typeof(string))
            {
                result = ToString();
                return true;
            }
            return base.TryConvert(binder, out result);
        }
        /// <summary>
        /// Converts this proxy to the specified Haxe virtual type.
        /// </summary>
        /// <param name="t">The virtual proxy type to convert to.</param>
        /// <returns>A proxy of type <paramref name="t"/>.</returns>
        public virtual HaxeVirtual ToVirtual(Type t)
        {
            var tid = HaxeProxyManager.type2typeId[t];
            var vt = HashlinkMarshal.Module.PreferTypes[tid];
            var result = (HashlinkVirtual)HashlinkMarshal.ConvertHashlinkObject(
                HashlinkNative.hl_to_virtual(vt.NativeType, (HL_vdynamic*)HashlinkPointer)
                )!;
            return result.AsHaxe<HaxeVirtual>();
        }
        /// <summary>
        /// Converts this proxy to the specified Haxe virtual type.
        /// </summary>
        /// <typeparam name="T">The virtual proxy type to convert to.</typeparam>
        /// <returns>A proxy of type <typeparamref name="T"/>.</returns>
        public T ToVirtual<T>() where T : HaxeVirtual
        {
            return (T) ToVirtual(typeof(T));
        }

        /// <summary>
        /// Gets the Haxe object represented by this proxy.
        /// </summary>
        /// <returns>The represented Haxe object.</returns>
        /// <exception cref="InvalidCastException">This proxy cannot be converted to an object.</exception>
        public HaxeObject AsObject()
        {
            if (this is HaxeObject result)
            {
                return result;
            }
            if (this is HaxeVirtual)
            {
                return ((HashlinkVirtual)HashlinkObj).GetValue()?.AsHaxe<HaxeObject>()
                    ?? throw new InvalidCastException();
            }
            throw new InvalidCastException();
        }

        /// <summary>
        /// Gets the Haxe object represented by this proxy.
        /// </summary>
        /// <typeparam name="T">The expected object type.</typeparam>
        /// <returns>The represented object cast to <typeparamref name="T"/>.</returns>
        /// <exception cref="InvalidCastException">This proxy cannot be converted to <typeparamref name="T"/>.</exception>
        public virtual T AsObject<T>() where T : HaxeObject
        {
            if (this is T result)
            {
                return result;
            }
            if (this is HaxeVirtual)
            {
                return ((HashlinkVirtual)HashlinkObj).GetValue()?.AsHaxe<T>()
                    ?? throw new InvalidCastException();
            }
            throw new InvalidCastException();
        }
        T IExtraData.GetOrCreateData<T>( Func<HashlinkObj, object> factory ) where T : class
        {
            return ((IExtraData)HashlinkObj).GetOrCreateData<T>(factory);
        }
    }
}
