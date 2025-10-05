
using Hashlink.Proxy.Clousre;
using Hashlink.Reflection.Types;
using Hashlink.Wrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Reflection.Members
{
    public unsafe class HashlinkNativeFunction( HashlinkModule module, HL_native* func ) : HashlinkMember(module, func),
        IHashlinkMemberGenerator,
        IHashlinkFunc
    {
        private string? cachedName;
        private HashlinkFuncType? cachedFuncType;
        private Delegate? cachedDynInvoke;

        public int FunctionIndex => func->findex;
        public nint EntryPointer => (nint)Module.NativeModule->functions_ptrs[
             FunctionIndex
            ];

        public override string? Name => cachedName ??= Marshal.PtrToStringAnsi((nint)func->name);
        public HashlinkFuncType FuncType => cachedFuncType ??= GetMemberFrom<HashlinkFuncType>(func->type);

        public HashlinkClosure CreateClosure( nint entry = 0 )
        {
            return FuncType.CreateClosure(entry == 0 ? EntryPointer : entry);
        }
        public Delegate CreateDelegate( Type type )
        {
            return HashlinkWrapperFactory.GetWrapper(
                FuncType, EntryPointer, type);
        }
        public T CreateDelegate<T>() where T : Delegate
        {
            return (T)CreateDelegate(typeof(T));
        }
        public object? DynamicInvoke( params object?[]? args )
        {
            cachedDynInvoke ??= HashlinkWrapperFactory.GetWrapper(
                FuncType, EntryPointer);
            return cachedDynInvoke.DynamicInvoke(args);
        }
        static HashlinkMember IHashlinkMemberGenerator.GenerateFromPointer( HashlinkModule module, void* ptr )
        {
            return new HashlinkNativeFunction(module, (HL_native*)ptr);
        }
    }
}
