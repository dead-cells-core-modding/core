using Hashlink.Marshaling;
using Hashlink.Proxy;
using Hashlink.Proxy.Objects;
using Hashlink.Reflection.Members;
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
    public unsafe class HashlinkObjectType(HashlinkModule module, HL_type* type) : 
        HashlinkSpecialType<HL_type_obj>(module, type)
    {
        private HashlinkObjectType? cachedSuper;
        private int? cachedFieldsCount;
        private int? cachedProtosCount;
        private string? cachedName;
        private HashlinkObjectField[]? cachedFields;
        private HashlinkObjectProto[]? cachedProtos;
        private HashlinkObjectBinding[]? cachedBindings;
        private HashlinkObject? cachedGlobalValue;

        private readonly ConcurrentDictionary<string, HashlinkObjectField?> cachedFieldLookup = [];
        private readonly ConcurrentDictionary<string, HashlinkObjectProto?> cachedProtoLookup = [];
        public HashlinkObjectType? Super => TypeData->super != null ?
            cachedSuper ??= GetMemberFrom<HashlinkObjectType>(TypeData->super) :
            null;
        public override string Name => cachedName ??= new(TypeData->name);
        public override HashlinkObj CreateInstance()
        {
            return new HashlinkObject(this);
        }

        public int TotalPrototsCount => cachedProtosCount ??= (Super?.TotalPrototsCount ?? 0) + TypeData->nproto;
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
        public HashlinkObjectBinding[] Bindings
        {
            get
            {
                if (cachedBindings == null)
                {
                    cachedBindings = new HashlinkObjectBinding[TypeData->nbindings];
                    for (int i = 0; i < TypeData->nbindings; i++)
                    {
                        cachedBindings[i] = new(Module, TypeData->bindings + i * 2, this);
                    }
                }
                return cachedBindings;
            }
        }
        public int TotalFieldsCount => cachedFieldsCount ??= (Super?.TotalFieldsCount ?? 0) + TypeData->nfields;
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
                        cachedFields[i].Index = (Super?.TotalFieldsCount ?? 0) + i;
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
            if (field == null && Super != null)
            {
                return Super.TryFindField(name, out field);
            }
            return field != null;
        }

        private HashlinkObjectField? FindFieldByIdImpl( ref int idx )
        {
            var result = Super?.FindFieldByIdImpl(ref idx);
            if (result != null)
            {
                return result;
            }
            if (idx < Fields.Length)
            {
                return Fields[idx];
            }
            else
            {
                idx -= Fields.Length;
                return null;
            }
        }
        public HashlinkObjectField? FindFieldById( int idx )
        {
            return FindFieldByIdImpl(ref idx);
        }

        private HashlinkObjectProto? FindProtodByIdImpl( ref int idx )
        {
            var result = Super?.FindProtodByIdImpl(ref idx);
            if (result != null)
            {
                return result;
            }
            if (idx < Protos.Length)
            {
                return Protos[idx];
            }
            else
            {
                idx -= Protos.Length;
                return null;
            }
        }
        public HashlinkObjectProto? FindProtoById( int idx )
        {
            return FindProtodByIdImpl(ref idx);
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
            if (field == null && Super != null)
            {
                return Super.TryFindProto(name, out field);
            }
            return field != null;
        }
        
    }
}
