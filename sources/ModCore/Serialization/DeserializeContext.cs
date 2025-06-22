using dc.hxbit;
using HaxeProxy.Runtime;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ModCore.Serialization.SerializeContext;

namespace ModCore.Serialization
{
    internal record class DeserializeContext( Serializer Serializer )
    {
        private static readonly Dictionary<System.Type, FastReflectionHelper.FastInvoker> setDataInvoker = [];

        public static DeserializeContext? current;
        public static readonly Stack<DeserializeContext> stack = [];

        public readonly Dictionary<int, HaxeObject> hxbitObjects = [];

        [MemberNotNull(nameof(current))]
        public static void PushContext( DeserializeContext ctx )
        {
            if (current != null)
            {
                stack.Push(current);
            }
            current = ctx;
        }
        public static void PopContext()
        {
            if (stack.Count == 0)
            {
                current = null;
                return;
            }
            current = stack.Pop();
        }
    }
}
