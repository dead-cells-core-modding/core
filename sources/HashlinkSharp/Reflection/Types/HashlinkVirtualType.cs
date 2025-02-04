using Hashlink.Reflection.Members.Object;
using Hashlink.Reflection.Members.Virtual;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Reflection.Types
{
    public unsafe class HashlinkVirtualType(HashlinkModule module, HL_type* type) : 
        HashlinkSpecialType<HL_type_virtual>(module, type)
    {
        private HashlinkVirtualField[]? cachedFields;
        private readonly ConcurrentDictionary<string, HashlinkVirtualField?> cachedFieldLookup = [];
        public HashlinkVirtualField[] Fields
        {
            get
            {
                if (cachedFields == null)
                {
                    cachedFields = new HashlinkVirtualField[TypeData->nfields];
                    for (int i = 0; i < TypeData->nfields; i++)
                    {
                        cachedFields[i] = GetMemberFrom<HashlinkVirtualField>(TypeData->fields + i);
                    }
                }
                return cachedFields;
            }
        }

        public bool HasField( string name )
        {
            return TryFindField(name, out _);
        }
        public HashlinkVirtualField? FindField( string name )
        {
            return TryFindField(name, out var field) ? field : null;
        }
        public bool TryFindField( string name, [NotNullWhen(true)] out HashlinkVirtualField? field )
        {
            field = cachedFieldLookup.GetOrAdd(name, name =>
            {
                return Fields.FirstOrDefault(x => x.Name == name);
            });
            return field != null;
        }
    }
}
