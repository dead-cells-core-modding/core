using Hashlink.Proxy.Clousre;
using Hashlink.Reflection.Members;
using Hashlink.Reflection.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Patch
{
    internal class CustomHashlinkFunction : IHashlinkFunc
    {
        public nint Pointer
        {
            get; set;
        }

        public int FunctionIndex
        {
            get; set;
        }

        HashlinkFuncType IHashlinkFunc.FuncType => throw new NotImplementedException();

        HashlinkClosure IHashlinkFunc.CreateClosure( nint entry )
        {
            throw new NotImplementedException();
        }

        Delegate IHashlinkFunc.CreateDelegate( Type type )
        {
            throw new NotImplementedException();
        }

        T IHashlinkFunc.CreateDelegate<T>()
        {
            throw new NotImplementedException();
        }

        object? IHashlinkFunc.DynamicInvoke( params object?[]? args )
        {
            throw new NotImplementedException();
        }
    }
}
