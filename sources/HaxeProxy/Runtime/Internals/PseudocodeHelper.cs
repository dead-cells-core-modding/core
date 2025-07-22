using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaxeProxy.Runtime.Internals
{
    internal class PseudocodeHelper
    {
        public static Delegate GetNativeMethod( string libName, string funcName )
        {
            throw new NotSupportedException();
        }
        public static HaxeVirtual ToVirtual( HaxeProxyBase obj )
        {
            throw new NotSupportedException();
        }
        public static object DynGet( object obj, string name )
        {
            throw new NotSupportedException();
        }
        public static void DynSet( object obj, string name, object value )
        {
            throw new NotSupportedException();
        }
        public static object CreateObject<T>()
        {
            throw new NotSupportedException();
        }
        public static object CreateClosure<T>(object self, RuntimeMethodHandle method)
        {
            throw new NotSupportedException();
        }
        public static T ReadMem<T>( nint ptr, int offset )
        {
            throw new NotSupportedException();
        }
        public static void WriteMem<T>( nint ptr, int offset, T value )
        {
            throw new NotSupportedException();
        }
    }
}
