using Hashlink.Reflection.Members;
using Hashlink.Reflection.Members.Object;
using Hashlink.Reflection.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaxeProxy.Runtime.Internals.Cache
{
    public struct ObjFieldInfoCache
    {
        public bool hasCache;
        public HashlinkType? field;
        public nint offset;
    }
}
