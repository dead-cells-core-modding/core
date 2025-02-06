using Hashlink.Marshaling;
using Hashlink.Proxy;
using Hashlink.Proxy.Objects;
using Hashlink.Reflection.Members;
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
    public unsafe class HashlinkObjectType(HashlinkModule module, HL_type* type) : 
        HashlinkSpecialType<HL_type_obj>(module, type)
    {
        private HashlinkObjectType? cachedSuper;
        private string? cachedName;
        private HashlinkObjectField[]? cachedFields;
        private HashlinkObjectProto[]? cachedProtos;
        private HashlinkObject? cachedGlobalValue;

        private readonly ConcurrentDictionary<string, HashlinkObjectField?> cachedFieldLookup = [];
        private readonly ConcurrentDictionary<string, HashlinkObjectProto?> cachedProtoLookup = [];
        public HashlinkObjectType? Super => cachedSuper ??= GetMemberFrom<HashlinkObjectType>(TypeData->super);
        public override string Name => cachedName ??= new(TypeData->name);
        public override HashlinkObj CreateInstance()
        {
            return new HashlinkObject(this);
        }

        public HashlinkObjectProto[] Protos
        {
            get
            {
                if (cachedProtos == null)
                {
                    cachedProtos = new HashlinkObjectProto[TypeData->nproto];
                    for (int i = 0; i < TypeData->nproto; i++)
                    {
                        cachedProtos[i] = GetMemberFrom<HashlinkObjectProto>(TypeData->proto + i);
                    }
                }
                return cachedProtos;
            }
        }
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
                    }
                }
                return cachedFields;
            }
        }
        public HashlinkObject? GlobalValue => (nint)TypeData->global_value == 0 ? null :
            cachedGlobalValue ??= HashlinkMarshal.ConvertHashlinkObject<HashlinkObject>(*TypeData->global_value);
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

        public bool HasProto( string name )
        {
            return TryFindProto(name, out _);
        }
        public HashlinkObjectProto? FindProto( string name )
        {
            return TryFindProto(name, out var field) ? field : null;
        }
        public bool TryFindProto( string name, [NotNullWhen(true)] out HashlinkObjectProto? field )
        {
            field = cachedProtoLookup.GetOrAdd(name, name =>
            {
                return Protos.FirstOrDefault(x => x.Name == name);
            });
            return field != null;
        }
        
    }
}
