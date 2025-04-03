using Hashlink.Proxy.Clousre;
using Hashlink.Reflection.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Reflection.Members
{
    public interface IHashlinkFunc
    {
        int FunctionIndex
        {
            get;
        }
        HashlinkFuncType FuncType
        {
            get;
        }

        HashlinkClosure CreateClosure( nint entry = 0 );
        Delegate CreateDelegate( Type type );
        T CreateDelegate<T>() where T : Delegate;
        object? DynamicInvoke( params object?[]? args );
    }
}
