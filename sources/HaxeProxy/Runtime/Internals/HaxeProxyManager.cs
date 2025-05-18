using Hashlink.Marshaling;
using Hashlink.Proxy;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace HaxeProxy.Runtime.Internals
{
    internal static unsafe class HaxeProxyManager
    {
        private static readonly delegate*< HaxeProxyBase, HashlinkObj, void > baseCtor =
            (delegate*< HaxeProxyBase, HashlinkObj, void > )
                typeof(HaxeProxyBase).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).First().MethodHandle.GetFunctionPointer();
        private static Type[] bindingTypes = [];
        public static void Initialize( Assembly proxyAssembly )
        {
            bindingTypes = new Type[HashlinkMarshal.Module.Types.Length];
            var types = proxyAssembly.GetCustomAttributes<HaxeProxyBindingAttribute>();
            foreach (var v in types)
            {
                bindingTypes[v.TypeIndex] = v.Type;
            }
        }

        public static HaxeProxyBase CreateProxy( HashlinkObj obj )
        {
            var ht = obj.Type;
            if (ht.TypeIndex < 0)
            {
                throw new NotSupportedException();
            }
            var pt = bindingTypes[ht.TypeIndex];

            Debug.Assert(pt != null);
            Debug.Assert(!pt.IsAbstract);

            var inst = (HaxeProxyBase)RuntimeHelpers.GetUninitializedObject(pt);
            inst.createByManager = true;
            baseCtor(inst, obj);
            return inst;
        }
    }
}
