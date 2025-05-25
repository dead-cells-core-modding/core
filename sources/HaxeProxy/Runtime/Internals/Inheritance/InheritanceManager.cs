using Hashlink.Reflection.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HaxeProxy.Runtime.Internals.Inheritance
{
    internal static class InheritanceManager
    {
        private static readonly ReaderWriterLockSlim rwLock = new();
        private static readonly HashSet<Type> processed = [];
        private static readonly Dictionary<HashlinkObjectType, Dictionary<string, ProtoOverride>> overrideMethodsDict = [];

        private static void ProcessType( Type type, HashlinkObjectType otype )
        {
            Type curType = type;
            List<string> overrideMethods = [];
            while (!HaxeProxyManager.knownProxyTypes.Contains(curType))
            {
                foreach (var v in curType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    if (!v.IsVirtual)
                    {
                        continue;
                    }
                    overrideMethods.Add(v.Name);
                }
                Debug.Assert(curType.BaseType != null);
                curType = curType.BaseType;
            }
            if (!overrideMethodsDict.TryGetValue(otype, out var dict))
            {
                dict = [];
                overrideMethodsDict.Add(otype, dict);
            }
            foreach (var v in overrideMethods)
            {
                if (dict.TryGetValue(v, out var po))
                {
                    continue;
                }
                var proto = otype.FindProto(v) ??
                    throw new MissingMethodException(otype.Name, v);
                po = new(proto, otype, curType.GetMethod(v) ??
                    throw new MissingMethodException(curType.FullName, v));
                dict.Add(v, po);
            }
        }

        public static void Check( Type type, HashlinkObjectType otype )
        {
            rwLock.EnterReadLock();
            if (processed.Contains(type))
            {
                rwLock.ExitReadLock();
                return;
            }
            rwLock.ExitReadLock();
            rwLock.EnterWriteLock();

            if (processed.Contains(type))
            {
                rwLock.ExitWriteLock();
                return;
            }
            try
            {
                ProcessType(type, otype);
                processed.Add(type);
            }
            finally
            {
                rwLock.ExitWriteLock();
            }
        }

    }
}
