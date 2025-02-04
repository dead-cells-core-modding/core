using Hashlink.Marshaling;
using Hashlink.Reflection.Types;
using System.Collections.Concurrent;

namespace Hashlink.Brigde
{
    public sealed unsafe class MethodWrapper : IDisposable
    {
        public delegate object? WrapperEntry( MethodWrapper wrapper, object?[] args );
        private bool disposedValue;

        private static readonly ConcurrentDictionary<Type, Func<object[], object>> targetCaller = [];
        private readonly WrapperEntry target;
        private MethodWrapperFactory.EntryItem* entry;
        public HashlinkType ReturnType
        {
            get;
        }
        public HashlinkType[] ArgTypes
        {
            get;
        }

        public object? Data
        {
            get; set;
        }
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

        public MethodWrapper( WrapperEntry target,
            HashlinkType retType,
            HashlinkType[] argTypes )
        {
            ReturnType = retType;
            ArgTypes = argTypes;
            this.target = target;
            entry = MethodWrapperFactory.CreateWrapper(this, argTypes, retType);
        }

        internal void Entry( MethodWrapperFactory.NativeInfoTable* table, void* retVal, long* argPtr )
        {
            var args = new object?[table->argsCount];
            for (var i = 0; i < table->argsCount; i++)
            {
                args[i] = HashlinkMarshal.ReadData(argPtr + i, ArgTypes[i]);
            }

            var ret = target(this, args);
            if (ret is float fret)
            {
                *(double*)retVal = fret;
            }
            else
            {
                HashlinkMarshal.WriteData(retVal, ret, ReturnType);
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
