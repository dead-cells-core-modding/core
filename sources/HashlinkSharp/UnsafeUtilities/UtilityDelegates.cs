using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MonoMod.Utils;

namespace Hashlink.UnsafeUtilities
{
    static class UtilityDelegates
    {
        private static readonly ModuleBuilder moduleBuilder = AssemblyBuilder.DefineDynamicAssembly(
            new("UtilityDelegates"), AssemblyBuilderAccess.Run).DefineDynamicModule("MainModule");

        private static readonly MethodInfo MI_generalDelegateFunc_Invoke = typeof(Func<object?[], object?>).GetMethod("Invoke")!;

        private static readonly ConcurrentDictionary<int, Type> anonymousGenericDelegates = [];
        private static readonly ConcurrentDictionary<MethodInfo, Type> anonymousGenericInstDelegates = [];
        private static readonly ConcurrentDictionary<MethodInfo, Type> anonymousDelegateTypes = [];
        private static readonly ConcurrentDictionary<Type, MethodInfo> adaptDelegatesEx = [];
        private static readonly ConcurrentDictionary<Type, MethodInfo> adaptDelegates = [];
        private static readonly ConcurrentDictionary<Type, MethodInfo> closureDelegates = [];

        private static Type CreateGenericDelegate( int argCount )
        {
            var s = argCount;
            var hasRet = (argCount & 1) == 1;
            argCount >>= 1;

            var gpNames = new string[argCount + (hasRet ? 1 : 0)];
            if (hasRet)
            {
                gpNames[^1] = "TReturn";
            }
            for (int i = 0; i < argCount; i++)
            {
                gpNames[i] = "TArg" + i;
            }

            var typeBuilder = moduleBuilder.DefineType(
            "AnonymousDelegate" + s,
            TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed,
            typeof(MulticastDelegate));

            var gp = typeBuilder.DefineGenericParameters(gpNames);

            typeBuilder.DefineConstructor(
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                CallingConventions.Standard,
               [ typeof(object), typeof(IntPtr) ])
                .SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

            typeBuilder.DefineMethod(
                "Invoke",
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                hasRet ? gp[^1] : typeof(void),
                hasRet ? gp[..^1] : gp
                ).
                SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

            typeBuilder.DefineMethod(
               "BeginInvoke",
               MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
               typeof(IAsyncResult),
               [..(hasRet ? gp[..^1] : gp), typeof(AsyncCallback), typeof(object)]
               ).
               SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

            typeBuilder.DefineMethod(
              "EndInvoke",
              MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
              typeof(void),
              [typeof(IAsyncResult)]
              ).
              SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

            return typeBuilder.CreateType();
        }
        private static Type CreateDelegate( string name, Type ret, params Type[] args )
        {
            var typeBuilder = moduleBuilder.DefineType(
            name,
            TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed,
            typeof(MulticastDelegate));

            typeBuilder.DefineConstructor(
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                CallingConventions.Standard,
               [typeof(object), typeof(IntPtr)])
                .SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

            typeBuilder.DefineMethod(
                "Invoke",
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                ret,
                args
                ).
                SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

            typeBuilder.DefineMethod(
               "BeginInvoke",
               MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
               typeof(IAsyncResult),
               [..args, typeof(AsyncCallback), typeof(object)]
               ).
               SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

            typeBuilder.DefineMethod(
              "EndInvoke",
              MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
              ret,
              [typeof(IAsyncResult)]
              ).
              SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

            return typeBuilder.CreateType();
        }
        private static Type CreateMethodDelegate( MethodInfo m )
        {
            var hasRet = m.ReturnType != typeof(void);
            var pte = m is DynamicMethod dm ? dm.GetParameters().Skip(1).Select(x => x.ParameterType) :
                 m.GetParameters().Select(x => x.ParameterType);
            Type[] ptypes = hasRet ? [.. pte, m.ReturnType] : [.. pte];
            var pcount = hasRet ? ptypes.Length - 1 : ptypes.Length;

            if (ptypes.Length == 0)
            {
                return typeof(Action);
            }
            var dtype = anonymousGenericDelegates.GetOrAdd(
                pcount << 1 | (hasRet ? 1 : 0), CreateGenericDelegate);
            return dtype.MakeGenericType(ptypes);
        }
        private static Type CreateMethodDelegateNoGeneric( MethodInfo m )
        {
            return CreateDelegate("Delegate+" + m.Name, m.ReturnType, 
                [
                    ..m.GetParameters().Skip(m is DynamicMethod ? 1 : 0).Select(x => x.ParameterType)
                ]);
        }
        public static Delegate CreateAnonymousDelegate( this MethodInfo info, object? target, 
            bool noGeneric = false )
        {

            foreach (var v in info.GetParameters())
            {
                if (v.ParameterType.IsByRefLike())
                {
                    noGeneric = true;
                }
            }
            if (noGeneric)
            {
                return info.CreateDelegate(anonymousDelegateTypes.GetOrAdd(
                    info, CreateMethodDelegateNoGeneric
                    ), target);
            }
            else
            {
                return info.CreateDelegate(anonymousGenericInstDelegates.GetOrAdd(
                    info, CreateMethodDelegate
                    ), target);
            }
        }
        private static MethodInfo CreateAdaptDelegateEx( Type type )
        {
            var invoke = type.GetMethod("Invoke")!;
            var ps = invoke.GetParameters();

            var dm = new DynamicMethod("GeneralDelegate+" + type.Name, invoke.ReturnType,
                [typeof(Delegate), .. ps.Select(x => x.ParameterType)], true);
            var ilg = dm.GetILGenerator();
            ilg.Emit(OpCodes.Ldarg_0);
            ilg.Emit(OpCodes.Ldc_I4, ps.Length);
            ilg.Emit(OpCodes.Newarr, typeof(object));
            for (int i = 0; i < ps.Length; i++)
            {
                ilg.Emit(OpCodes.Dup);
                ilg.Emit(OpCodes.Ldc_I4, i);
                ilg.Emit(OpCodes.Ldarg, i + 1);
                ilg.Emit(OpCodes.Box, ps[i].ParameterType);
                ilg.Emit(OpCodes.Stelem_Ref);
            }
            ilg.Emit(OpCodes.Callvirt, MI_generalDelegateFunc_Invoke);
            if (dm.ReturnType == typeof(void))
            {
                ilg.Emit(OpCodes.Pop);
            }
            else
            {
                if (dm.ReturnType != typeof(object))
                {
                    ilg.Emit(OpCodes.Unbox_Any, dm.ReturnType);
                }
            }
            ilg.Emit(OpCodes.Ret);
            return dm;
        }
        private static MethodInfo CreateAdaptDelegate( Type type )
        {
            var invoke = type.GetMethod("Invoke")!;
            var ps = invoke.GetParameters();
            Type[] ts = [.. ps.Select(x => x.ParameterType)];

            var dm = new DynamicMethod("AdaptDelegate+" + type.Name, invoke.ReturnType,
                [typeof(DelegateInfo), ..ts ], true);
            var ilg = dm.GetILGenerator();

            ilg.Emit(OpCodes.Ldarg_0);
            ilg.Emit(OpCodes.Ldfld, DelegateInfo.FI_self);

            for (int i = 0; i < ts.Length; i++)
            {
                ilg.Emit(OpCodes.Ldarg, i + 1);
            }

            ilg.Emit(OpCodes.Ldarg_0);
            ilg.Emit(OpCodes.Ldfld, DelegateInfo.FI_invokePtr);
            ilg.EmitCalli(OpCodes.Calli, CallingConventions.HasThis, invoke.ReturnType, ts, null);
            ilg.Emit(OpCodes.Ret);
            return dm;
        }
        public static Delegate CreateAdaptDelegateEx( Type delegateType, Func<object?[], object?> target )
        {
            return adaptDelegatesEx.GetOrAdd(delegateType, CreateAdaptDelegateEx).CreateDelegate(delegateType, target);
        }
        public static Delegate CreateAdaptDelegate( this Delegate target, Type targetType )
        {
            if (targetType.IsAssignableFrom(target.GetType()))
            {
                return target;
            }
            return adaptDelegates.GetOrAdd(targetType, CreateAdaptDelegate).CreateDelegate(
                targetType, new DelegateInfo(target));
        }
        public static T CreateAdaptDelegate<T>( this Delegate target) where T : Delegate
        {
            return (T) target.CreateAdaptDelegate(typeof(T));
        }
        private static MethodInfo CreateClosureDelegate( Type type )
        {
            var invoke = type.GetMethod("Invoke")!;
            var ps = invoke.GetParameters();
            Type[] ts = [.. ps.Skip(1).Select(x => x.ParameterType)];

            var dm = new DynamicMethod("BindDelegate+" + type.Name, invoke.ReturnType,
                [typeof(ClosureInfo), .. ts], true);
            var ilg = dm.GetILGenerator();

            ilg.Emit(OpCodes.Ldarg_0);
            ilg.Emit(OpCodes.Ldfld, ClosureInfo.FI_target);
            ilg.Emit(OpCodes.Ldfld, DelegateInfo.FI_self);

            ilg.Emit(OpCodes.Ldarg_0);
            ilg.Emit(OpCodes.Ldfld, ClosureInfo.FI_first);

            for (int i = 0; i < ts.Length; i++)
            {
                ilg.Emit(OpCodes.Ldarg, i + 1);
            }

            ilg.Emit(OpCodes.Ldarg_0);
            ilg.Emit(OpCodes.Ldfld, ClosureInfo.FI_target);
            ilg.Emit(OpCodes.Ldfld, DelegateInfo.FI_invokePtr);
            ilg.EmitCalli(OpCodes.Calli, CallingConventions.HasThis, invoke.ReturnType,
                [ps[0].ParameterType, ..ts], 
                null);
            ilg.Emit(OpCodes.Ret);
            return dm;
        }
        public static Delegate Bind( this MethodInfo target, object? self, Type? targetType = null )
        {
            if (targetType != null)
            {
                return target.CreateDelegate(targetType, self);
            }
            else
            {
                return target.CreateAnonymousDelegate(self);
            }
        }
        public static Delegate Bind( this Delegate target, object? self, Type? targetType = null )
        {
            var cd = closureDelegates.GetOrAdd(target.GetType(), CreateClosureDelegate);
            var ci = new ClosureInfo()
            {
                first = self,
                target = new(target)
            };
            if (targetType != null)
            {
                return cd.CreateDelegate(targetType, ci);
            }
            else
            {
                return cd.CreateAnonymousDelegate(ci);
            }
        }
        public static T Bind<T>( this Delegate target, object? self) where T : Delegate
        {
            return (T) target.Bind(self, typeof(T));
        }
    }
}
