using Hashlink.Marshaling;
using Hashlink.Reflection.Members;
using Hashlink.Reflection.Types;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Hashlink.Reflection.HashlinkModule;

namespace Hashlink.Reflection
{
    public unsafe class HashlinkModule
    {
        private static readonly ConcurrentDictionary<nint, HashlinkMemberHandle> cachedHandles = [];

        private static readonly Dictionary<string, HashlinkType> typeNameMapping = [];

        public HashlinkType[] Types
        {
            get;
        }
        public HashlinkFunction[] Functions
        {
            get;
        }

        public HL_module* NativeModule
        {
            get;
        }
        public HL_code* NativeCode => NativeModule->code;

       

        public class KnownType(HashlinkModule module)
        {
            public HashlinkType String
            {
                get;
            } = module.GetTypeByName("String");
            public HashlinkType Void
            {
                get;
            } = module.GetTypeByName("void");
            public HashlinkType I8
            {
                get;
            } = module.GetTypeByName("i8");
            public HashlinkType I16
            {
                get;
            } = module.GetTypeByName("i16");
            public HashlinkType I32
            {
                get;
            } = module.GetTypeByName("i32");
            public HashlinkType I64
            {
                get;
            } = module.GetTypeByName("i64");
            public HashlinkType F32
            {
                get;
            } = module.GetTypeByName("f32");
            public HashlinkType F64
            {
                get;
            } = module.GetTypeByName("f64");
            public HashlinkType Bool
            {
                get;
            } = module.GetTypeByName("bool");
            public HashlinkType Bytes
            {
                get;
            } = module.GetTypeByName("bytes");
            public HashlinkType Dynamic
            {
                get;
            } = module.GetTypeByName("dynamic");
            public HashlinkType Array
            {
                get;
            } = module.GetTypeByName("array");
            public HashlinkType Type
            {
                get;
            } = module.GetTypeByName("type");
            public HashlinkType DynObj
            {
                get;
            } = module.GetTypeByName("dynobj");
        }

        public KnownType KnownTypes
        {
            get;
        }

        public HashlinkModule( HL_module* module )
        {
            NativeModule = module;

            Types = new HashlinkType[NativeCode->ntypes];
            for (int i = 0; i < NativeCode->ntypes; i++)
            {
                var type = GetMemberFrom<HashlinkType>(NativeCode->types + i);
                Types[i] = type;
                type.TypeIndex = i;
                if (type.Name != null)
                {
                    typeNameMapping[type.Name] = type;
                }
                
            }

            KnownTypes = new(this);

            Functions = new HashlinkFunction[NativeCode->nfunctions];
            for (int i = 0; i < NativeCode->nfunctions; i++)
            {
                Functions[i] = GetMemberFrom<HashlinkFunction>(NativeCode->functions + i);
            }
        }

        public HashlinkFunction GetFunctionByFIndex( int findex )
        {
            return Functions[
                NativeModule->functions_indexes[findex]
                ];
        }
        public HashlinkType GetTypeByName( string name )
        {
            return typeNameMapping[name];
        }
        [return: NotNullIfNotNull(nameof(ptr))]
        public T? GetMemberFrom<T>( void* ptr ) where T : HashlinkMember, IHashlinkMemberGenerator
        {
            return (T?)GetMemberFrom(ptr) ?? (T)T.GenerateFromPointer(this, ptr);
        }
        [return: NotNullIfNotNull(nameof(ptr))]
        public T? GetMemberFrom<T>( void* ptr, Func<HashlinkModule, nint, T> factory ) where T : HashlinkMember
        {
            return (T?)GetMemberFrom(ptr) ?? factory(this, (nint)ptr);
        }
        public HashlinkMember? GetMemberFrom( void* ptr )
        {
            if (ptr == null)
            {
                return null;
            }
            var handle = GetHandle(ptr);
            if (handle?.Member != null)
            {
                return handle.Member;
            }
            return null;
        }

        private static readonly nint[] internalTypes = [
            (nint)InternalTypes.hlt_dyn,
            (nint)InternalTypes.hlt_f64,
            (nint)InternalTypes.hlt_f32,
            (nint)InternalTypes.hlt_abstract,
            (nint)InternalTypes.hlt_array,
            (nint)InternalTypes.hlt_bool,
            (nint)InternalTypes.hlt_bytes,
            (nint)InternalTypes.hlt_void,
            (nint)InternalTypes.hlt_i32,
            (nint)InternalTypes.hlt_f64,
            (nint)InternalTypes.hlt_dynobj,
            (nint)InternalTypes.hlt_array
            ];
        internal HashlinkMemberHandle? GetHandle( void* ptr )
        {
            if (cachedHandles.TryGetValue((nint)ptr, out var result))
            {
                return result;
            }
            if (!
                (   
                    Array.IndexOf(internalTypes, (nint)ptr) != -1 ||
                    Utils.InAllocBlock(NativeCode->alloc, ptr) ||
                    Utils.InAllocBlock(NativeModule->ctx.alloc, ptr)
                )
                )
            {
                return null;
            }
            return cachedHandles.GetOrAdd((nint)ptr, ptr =>
            {
                return new HashlinkMemberHandle((void*)ptr);
            });
        }
    }
}
