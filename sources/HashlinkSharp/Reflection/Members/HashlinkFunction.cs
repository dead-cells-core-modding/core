using Hashlink.Proxy.Clousre;
using Hashlink.Reflection.Types;
using Hashlink.Wrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Reflection.Members
{
    public unsafe class HashlinkFunction(HashlinkModule module, HL_function* func ) : HashlinkMember(module, func),
        IHashlinkMemberGenerator
    {
        private HashlinkObjectType? cachedDeclaringType;
        private HashlinkFuncType? cachedFuncType;
        private Delegate? cachedDynInvoke;

        public override HashlinkType? DeclaringType
        {
            get
            {
                if (cachedDeclaringType == null && func->obj != null)
                {
                    cachedDeclaringType = GetMemberFrom<HashlinkObjectType>(func->obj->rt->t);
                }
                return cachedDeclaringType;
            }
        }
        public int FunctionIndex => func->findex;
        public nint EntryPointer => (nint)Module.NativeModule->functions_ptrs[
             FunctionIndex
            ];

        public override string? Name => FuncType.Name;
        public HashlinkFuncType FuncType => cachedFuncType ??= GetMemberFrom<HashlinkFuncType>(func->type);

        public HashlinkClosure CreateClosure( nint entry = 0 )
        {
            return FuncType.CreateClosure(entry == 0 ? EntryPointer : entry);
        }
        public Delegate CreateDelegate( Type type )
        {
            return HashlinkWrapperFactory.GetWrapper(
                FuncType, EntryPointer, type );
        }
        public T CreateDelegate<T>( ) where T : Delegate
        {
            return (T)CreateDelegate(typeof(T));
        }
        public object? DynamicInvoke( params object?[]? args )
        {
            cachedDynInvoke ??= HashlinkWrapperFactory.GetWrapper(
                FuncType, EntryPointer );
            return cachedDynInvoke.DynamicInvoke(args);
        }
        static HashlinkMember IHashlinkMemberGenerator.GenerateFromPointer( HashlinkModule module, void* ptr )
        {
            return new HashlinkFunction(module, (HL_function *) ptr);
        }
    }
}
