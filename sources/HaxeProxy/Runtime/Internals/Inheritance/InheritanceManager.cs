using Hashlink.Marshaling;
using Hashlink.Reflection.Types;
using HaxeProxy.Events;
using ModCore.Events;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;


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
                    return (HashlinkObjectType)HashlinkMarshal.Module.PreferTypes[ca.Index];
                }
                t = t.BaseType;
            }
            throw new InvalidOperationException();
        }

        public static void Check( Type type, HashlinkObjectType? otype, [NotNull] out CustomHaxeType? cht )
        {
            RuntimeHelpers.RunClassConstructor(type.TypeHandle);

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
