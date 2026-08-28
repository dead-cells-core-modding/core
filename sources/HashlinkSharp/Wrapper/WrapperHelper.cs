using System.Diagnostics;
using System.Runtime.InteropServices;
using Hashlink.Marshaling;
using Hashlink.Proxy;
using Hashlink.Reflection.Types;
using ModCore.Events;

namespace Hashlink.Wrapper
{
    internal unsafe partial class WrapperHelper
    {
        private readonly static nint* TRAP_MAGIC_NUMBER = (nint*) NativeMemory.Alloc((nuint)sizeof(nint) * 4, 16);
        static WrapperHelper()
        {
            EventSystem.AddReceiver(new ExceptionEventHandler());

            ModCore.Native.Native.Current.Data->trap_magic_number = (nint)TRAP_MAGIC_NUMBER;
            *TRAP_MAGIC_NUMBER = 0x4e455445; // "NETE"
            *(TRAP_MAGIC_NUMBER + 1) = 0;
        }

        public static object? GetObjectFromPtr( nint ptr )
        {
            return HashlinkMarshal.ConvertHashlinkObject(HashlinkObjPtr.Get(ptr), null);
        }

        public static T GetObjectFrom<T>( object obj ) where T : class, IExtraDataItem
        {
            if (obj is T result)
            {
                return result;
            }
            if (obj is IExtraData ied)
            {
                return ied.GetData<T>();
            }
            return (T)(dynamic)obj;
        }

        public static nint AsPointer( object obj, int typeIdx )
        {
            return AsPointerWithType(obj, HashlinkMarshal.Module.PreferTypes[typeIdx]);
        }

        public static nint AsPointerWithType( object obj, HashlinkType type )
        {
            if (!type.IsPointer)
            {
                throw new InvalidOperationException();
            }

            nint result = 0;
            HashlinkMarshal.WriteData(&result, obj, type);
            return result;
        }

        public static void SetBlocking( bool b )
        {
            HashlinkMarshal.EnsureThreadRegistered();
            hl_blocking(b ? 1 : 0);
        }

        public static bool CheckBool( int b )
        {
            return b != 0;
        }
    }
}
