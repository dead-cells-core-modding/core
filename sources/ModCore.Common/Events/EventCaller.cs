using Mono.Cecil.Cil;
using MonoMod.Utils;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ModCore.Events
{
    internal delegate void ModuleEventCall( object self, nint refArg );
    internal static class EventCaller<TEvent>
    {
        private static readonly ModuleEventCall call;

        public static bool IsCalled
        {
            get; set;
        }
        public static bool IsCallOnce => Attribute.Once;
        public static EventAttribute Attribute
        {
            get;
        }
        public static MethodInfo EventMethod
        {
            get;
        }

        private static ModuleEventCall GenerateCall( MethodInfo method )
        {
            var @params = method.GetParameters();
            if (@params.Length >= 2)
            {
                throw new NotSupportedException("Methods with multiple parameters are not supported");
            }

            DynamicMethodDefinition caller = new($"ModuleEventCall+{method.Name}", typeof(void), [typeof(object), typeof(nint)]);
            var il = caller.GetILProcessor();
            il.Emit(OpCodes.Ldarg_0);
            if (@params.Length == 1)
            {
                il.Emit(OpCodes.Ldarg_1);
                if (!@params[0].ParameterType.IsByRef)
                {
                    il.Emit(OpCodes.Ldobj, @params[0].ParameterType);
                }
            }
            il.Emit(OpCodes.Callvirt, method);
            il.Emit(OpCodes.Ret);
            return caller.Generate().CreateDelegate<ModuleEventCall>();
        }

        private static MethodInfo FindEventMethod( Type type )
        {
            return type
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.DeclaredOnly)
                .Where(x => x.GetParameters().Length < 2)
                .First();
        }

        static EventCaller()
        {
            Attribute = typeof(TEvent).GetCustomAttribute<EventAttribute>() ??
                throw new InvalidOperationException();
            EventMethod = FindEventMethod(typeof(TEvent));
            call = GenerateCall(EventMethod);
        }

        public static void Invoke( TEvent self, nint refOfarg )
        {
            IsCalled = true;
            call(self!, refOfarg);
        }
        public static unsafe void Invoke<TArg>( TEvent self, ref TArg argOnStack ) where TArg : allows ref struct
        {
            Invoke(self!, (nint)Unsafe.AsPointer(ref argOnStack));
        }
        public static void Invoke( TEvent self )
        {
            Invoke(self!, 0);
        }
    }
}
