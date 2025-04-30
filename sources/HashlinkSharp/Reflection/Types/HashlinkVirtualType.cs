using Hashlink.Proxy;
using Hashlink.Proxy.Objects;
using Hashlink.Reflection.Members.Object;
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
        private HashlinkObjectField[]? cachedFields;
        private readonly ConcurrentDictionary<string, HashlinkObjectField?> cachedFieldLookup = [];
        public HashlinkObjectField[] Fields
        {
            get
            {
                if (cachedFields == null)
                {
                    cachedFields = new HashlinkObjectField[TypeData->nfields];
                    for (int i = 0; i < TypeData->nfields; i++)
                    {
                        cachedFields[i] = GetMemberFrom<HashlinkObjectField>(TypeData->fields + i);
                        cachedFields[i].Index = i;
                    }
                }
                return cachedFields;
            }
        }

        public override HashlinkObj CreateInstance()
        {
            return new HashlinkVirtual(this);
        }

        public bool HasField( string name )
        {
            return TryFindField(name, out _);
        }
        public HashlinkObjectField? FindField( string name )
        {
            return TryFindField(name, out var field) ? field : null;
        }
        public bool TryFindField( string name, [NotNullWhen(true)] out HashlinkObjectField? field )
        {
            field = cachedFieldLookup.GetOrAdd(name, name =>
            {
                return Fields.FirstOrDefault(x => x.Name == name);
            });
            return field != null;
        }
    }
}
