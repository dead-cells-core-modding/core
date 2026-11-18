
using Hashlink.Events.Interfaces;
using Hashlink.Marshaling;
using Hashlink.Proxy.Clousre;
using Hashlink.Reflection.Types;
using Hashlink.UnsafeUtilities;
using Hashlink.Wrapper;
using ModCore.Events;
using System.Reflection;
using System.Reflection.Emit;

namespace Hashlink.Reflection.Members
{
    public unsafe class HashlinkFunction( HashlinkModule module, HL_function* func ) : HashlinkMember(module, func),
        IHashlinkMemberGenerator,
        IHashlinkFunc
    {
        public const int FS_OFFSET_NATIVE_HOOK = 0;
        public const int FS_OFFSET_HASHLINK_HOOK = 12;
        public const int FS_OFFSET_MONOMOD_HOOK = 40;
        public const int FS_OFFSET_REAL_ENTRY = 52;

        private string? name;
        private HashlinkType[]? cachedLocalRegs;
        private HashlinkObjectType? cachedDeclaringType;
        private HashlinkFuncType? cachedFuncType;

        private Delegate? cachedDynInvoke;

        public override HashlinkType? DeclaringType {
            get {
                if (cachedDeclaringType == null && func->obj != null)
                {
                    cachedDeclaringType = GetMemberFrom<HashlinkObjectType>(func->obj->rt->t);
                }
                return cachedDeclaringType;
            }
        }
        public HashlinkType[] LocalRegisters {
            get {
                if (cachedLocalRegs == null)
                {
                    cachedLocalRegs = new HashlinkType[func->nregs];
                    for (int i = 0; i < func->nregs; i++)
                    {
                        cachedLocalRegs[i] = GetMemberFrom<HashlinkType>(func->regs[i]);
                    }
                }
                return cachedLocalRegs;
            }
        }

        public bool IsDotnetCompiled
        {
            get; internal set;
        }
        public int FunctionIndex => func->findex;
        public nint EntryPointer => (nint)Module.NativeModule->functions_ptrs[
             FunctionIndex
            ];

        private HashlinkClosure? dotnetWrapperClosure;

        internal void UseDotnetCompiledVersion(MethodInfo m, FieldInfo f_next_call_origin_function )
        {
            if (IsDotnetCompiled)
            {
                throw new InvalidOperationException();
            }
            IsDotnetCompiled = true;

            if (!m.IsStatic && m.DeclaringType != null)
            {
                var pc = m.GetParameters().Length + 1;
                DynamicMethod dm = new("dotnet_compiled_bridge", m.ReturnType, [m.DeclaringType,
                    ..m.GetParameters().Select(x => x.ParameterType)]);
                var il = dm.GetILGenerator();

                il.Emit(OpCodes.Ldc_I4, FunctionIndex);
                il.Emit(OpCodes.Stsfld, f_next_call_origin_function);
                for (int i = 0; i < pc; i++)
                {
                    il.Emit(OpCodes.Ldarg, i);
                }

                il.Emit(OpCodes.Call, m);
                il.Emit(OpCodes.Ret);

                m = dm;
            }
            else
            {
                if (!m.IsStatic && m.DeclaringType != null)
                {
                    var pc = m.GetParameters().Length;
                    DynamicMethod dm = new("dotnet_compiled_bridge", m.ReturnType, [..m.GetParameters().Select(x => x.ParameterType)]);
                    var il = dm.GetILGenerator();

                    il.Emit(OpCodes.Ldc_I4, FunctionIndex);
                    il.Emit(OpCodes.Stsfld, f_next_call_origin_function);
                    for (int i = 0; i < pc; i++)
                    {
                        il.Emit(OpCodes.Ldarg, i);
                    }

                    il.Emit(OpCodes.Call, m);
                    il.Emit(OpCodes.Ret);

                    m = dm;
                }
            }

            var ptr = HashlinkMarshal.GetDyn(m.CreateAnonymousDelegate(null, true));
            dotnetWrapperClosure = (HashlinkClosure) HashlinkMarshal.ReadData(&ptr, HashlinkMarshal.Module.KnownTypes.Dynamic)!;

            EventSystem.BroadcastEvent<IOnActivateDotnetCompiledCode, IOnActivateDotnetCompiledCode.Data>(new(this, m, 
                dotnetWrapperClosure.FunctionPtr));
        }

        public override string? Name {
            get {
                if (name == null)
                {
                    if (func->obj != null)
                    {
                        name = new string(func->field.field);
                    }
                }
                return name;
            }
        }
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
            return new HashlinkFunction(module, (HL_function*)ptr);
        }
    }
}
