using Hashlink.Marshaling;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Brigde
{
    public sealed unsafe class MethodWrapper : IDisposable
    {
        public delegate object? WrapperEntry(MethodWrapper wrapper, object?[] args);
        private bool disposedValue;

        private static readonly ConcurrentDictionary<Type, Func<object[], object>> targetCaller = [];
        private readonly WrapperEntry target;
        private MethodWrapperFactory.EntryItem* entry;

        public object? Data { get; set; }
        public nint EntryPointer => (nint)entry->table.entryPtr;
        public nint RedirectTarget
        {
            get
            {
                return entry->table.origFuncPtr;
            }
            set
            {
                entry->table.origFuncPtr = value;
            }
        }

        internal MethodWrapperFactory.EntryItem* EntryHandle => entry;

        public MethodWrapper(WrapperEntry target,
            HL_type.TypeKind retType, 
            IEnumerable<HL_type.TypeKind> argTypes)
        {
            this.target = target;
            entry = MethodWrapperFactory.CreateWrapper(this, argTypes, retType);
        }

        internal void Entry(MethodWrapperFactory.NativeInfoTable* table, void* retVal, long* argPtr)
        {
            var args = new object?[table->argsCount];
            for (int i = 0; i < table->argsCount; i++)
            {
                var at = (HL_type.TypeKind)table->targs[i];
                args[i] = HashlinkMarshal.ReadData(argPtr + i, at);
            }

            var ret = target(this, args);
            if (ret is float fret)
            {
                *(double*)retVal = fret;
            }
            else
            {
                HashlinkMarshal.WriteData(retVal, ret, (HL_type.TypeKind)table->retType);
            }
        }

        public void Dispose()
        {
            if (disposedValue)
            {
                return;
            }
            disposedValue = true;

            MethodWrapperFactory.FreeWrapper(this);

            entry = null;
        }
    }
}
