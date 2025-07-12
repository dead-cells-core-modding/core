using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink
{
    internal unsafe class FakeStackTraceManager
    {
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_stackTrace")]
        static extern ref object GetStackTrace(Exception ex);
        [StructLayout(LayoutKind.Sequential)]
        struct StackTraceElement
        {
            public nint ip;
            public nint sp;
            public nint pFunc;
            public int flags;
        }
        [StructLayout(LayoutKind.Sequential)]
        struct ArrayHeader
        {
            public int m_size;
            public int m_keepAliveItemsCount;
            public nint m_thread;
        };
        public record class RequestInfo(string ClassName, string FuncName);
        public record class FakeMethodInfo(MethodInfo Method, nint IP);
        private static readonly List<ModuleBuilder> builders = [];
        private static readonly Dictionary<RequestInfo, FakeMethodInfo> cachedMethods = [];

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static Dictionary<RequestInfo, FakeMethodInfo> RequestFakeMethods(params List<RequestInfo> requests)
        {
            var result = new Dictionary<RequestInfo, FakeMethodInfo>();
            for (var i = 0; i < requests.Count; i++)
            {
                if (cachedMethods.TryGetValue(requests[i], out var info))
                {
                    result[requests[i]] = info;
                    requests.RemoveAt(i);
                    i--;
                }
            }

            var dict = new Dictionary<string, List<string>>();
            foreach (var request in requests)
            {
                if(!dict.TryGetValue(request.ClassName, out var list))
                {
                    dict[request.ClassName] = list = [];
                }
                list.Add(request.FuncName);
            }

            foreach((var className, var funcList) in dict)
            {
                ModuleBuilder? builder = null;
                foreach(var v in builders)
                {
                    if(v.GetType(className, false, false) == null)
                    {
                        builder = v;
                    }
                }
                if(builder == null)
                {
                    var ab = AssemblyBuilder.DefineDynamicAssembly( new("Haxe_" + builders.Count), AssemblyBuilderAccess.Run );
                    builder = ab.DefineDynamicModule("Haxe_" + builders.Count);
                    builders.Add(builder);
                }
                var tb = builder.DefineType(className);
                foreach(var name in funcList)
                {
                    var fb = tb.DefineMethod(name, MethodAttributes.Public | MethodAttributes.Static);
                    var ilg = fb.GetILGenerator();
                    ilg.Emit(OpCodes.Ldnull);
                    ilg.Emit(OpCodes.Throw);
                }
                var type = tb.CreateType();
                foreach (var name in funcList)
                {
                    var method = type.GetMethod(name)!;
                    nint cip = 0;
                    try
                    {
                        //method.CreateDelegate<Action>()();
                    }
                    catch (NullReferenceException ex)
                    {
                        var st0 = (sbyte[])GetStackTrace(ex);
                        fixed (sbyte* b = st0)
                        {
                            var ste = (StackTraceElement*)(b + 16);
                            cip = ste[0].ip;
                        }
                    }
                    RequestInfo ri = new(className, name);
                    FakeMethodInfo fmi = new(method, cip);
                    result.Add(ri, fmi);
                    cachedMethods.Add(ri, fmi);
                }
            }

            return result;
        }
    }
}
