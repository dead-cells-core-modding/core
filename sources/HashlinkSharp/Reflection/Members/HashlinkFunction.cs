using Hashlink.Proxy.Clousre;
using Hashlink.Reflection.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Reflection.Members
{
    public unsafe class HashlinkFunction(HashlinkModule module, HL_function* func ) : HashlinkMember(module, func),
        IHashlinkMemberGenerator
    {
        private HashlinkObjectType? cachedDeclaringType;
        private HashlinkFuncType? cachedFuncType;

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
        public void* EntryPointer => Module.NativeModule->functions_ptrs[
             FunctionIndex
            ];

        public override string? Name => FuncType.Name;

        public HashlinkFuncType FuncType => cachedFuncType ??= GetMemberFrom<HashlinkFuncType>(func->type);
        public HashlinkFunc CreateFunc( void* entry = null )
        {
            return FuncType.CreateFunc(entry == null ? EntryPointer : entry);
        }
        public HashlinkClosure CreateClosure( void* entry = null )
        {
            return FuncType.CreateClosure(entry == null ? EntryPointer : entry);
        }
        static HashlinkMember IHashlinkMemberGenerator.GenerateFromPointer( HashlinkModule module, void* ptr )
        {
            return new HashlinkFunction(module, (HL_function *) ptr);
        }
    }
}
