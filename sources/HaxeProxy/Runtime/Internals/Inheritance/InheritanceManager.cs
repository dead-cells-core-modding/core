using Hashlink;
using Hashlink.Marshaling;
using Hashlink.Proxy.Objects;
using Hashlink.Reflection.Types;
using HaxeProxy.Events;
using ModCore.Collections;
using ModCore.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


namespace HaxeProxy.Runtime.Internals.Inheritance
{
    internal unsafe static class InheritanceManager
    {
        private static readonly ReaderWriterLockSlim rwLock = new();
        private static readonly Dictionary<Type, CustomHaxeType> processed = [];

        private static HashlinkObjectType FindHLType( Type type )
        {
            Type? t = type;
            while (t != null)
            {
                var ca = t.GetCustomAttribute<HashlinkTIndexAttribute>();
                if (ca != null)
                {
                    return (HashlinkObjectType) HashlinkMarshal.Module.Types[ca.Index];
                }
                t = t.BaseType;
            }
            throw new InvalidOperationException();
        }
        
        public static void Check( Type type, HashlinkObjectType? otype, [NotNull] out CustomHaxeType? cht )
        {
            rwLock.EnterReadLock();
            if (processed.TryGetValue(type, out cht))
            {
                rwLock.ExitReadLock();
                return;
            }
            rwLock.ExitReadLock();
            rwLock.EnterWriteLock();

            if (processed.TryGetValue(type, out cht))
            {
                rwLock.ExitWriteLock();
                return;
            }
            try
            {
                otype ??= FindHLType(type);
                cht = new(type, otype);
                processed.Add(type, cht);
            }
            finally
            {
                rwLock.ExitWriteLock();
            }
            EventSystem.BroadcastEvent<IOnRegisterCustomType, IOnRegisterCustomType.Data>(
                new(type, cht.Type, otype)
                );
        }

    }
}
