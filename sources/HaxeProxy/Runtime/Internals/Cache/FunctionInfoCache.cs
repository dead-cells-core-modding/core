using Hashlink.Reflection.Members;
using Hashlink.UnsafeUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaxeProxy.Runtime.Internals.Cache
{
    public struct FunctionInfoCache
    {
        public HashlinkFunction? function;
        public DelegateInfo? directEntry;
        public DelegateInfo? hookRealEntry;
    }
}
