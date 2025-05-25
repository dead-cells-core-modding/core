using Hashlink.Reflection.Members.Object;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaxeProxy.Runtime.Internals.Inheritance
{
    internal static class ProtoManager
    {
        private class ProtoInfo
        {
            public bool finished = false;

        }
        private static ConcurrentDictionary<HashlinkObjectProto, ProtoInfo> protos = [];


    }
}
