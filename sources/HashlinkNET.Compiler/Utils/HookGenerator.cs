using HashlinkNET.Compiler.Data;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace HashlinkNET.Compiler.Utils
{
    internal static class HookGenerator
    {

        public static TypeDefinition GenerateDelegateFor(
            MethodDefinition method,
            TypeSystem typeSystem,
            RuntimeImports rdata )
        {
            // Translated methods use 'ref T', but hook delegates keep the
            // HaxeProxy convention of Ref<T>.
            TypeReference MapParameterType( TypeReference type )
            {
                return type is ByReferenceType byRef
                    ? rdata.refType.MakeGenericInstanceType(byRef.ElementType)
                    : type;
            }

            var del = new TypeDefinition(
                null, null,
                TypeAttributes.NestedPublic | TypeAttributes.Sealed | TypeAttributes.Class,
                rdata.delegateBaseType
            );

            var ctor = new MethodDefinition(
                ".ctor",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.ReuseSlot,
                typeSystem.Void
            )
            {
                ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed,
                HasThis = true
            };
            ctor.Parameters.Add(new ParameterDefinition(typeSystem.Object));
            ctor.Parameters.Add(new ParameterDefinition(typeSystem.IntPtr));
            ctor.Body = new MethodBody(ctor);
            del.Methods.Add(ctor);

            var invoke = new MethodDefinition(
                "Invoke",
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                MapParameterType(method.ReturnType)
            )
            {
                ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed,
                HasThis = true
            };
            if (!method.IsStatic)
            {
                var selfType = method.DeclaringType;
                invoke.Parameters.Add(new ParameterDefinition("self", ParameterAttributes.None, selfType));
            }
            foreach (var param in method.Parameters)
                invoke.Parameters.Add(new ParameterDefinition(
                    param.Name,
                    param.Attributes & ~ParameterAttributes.Optional & ~ParameterAttributes.HasDefault,
                    MapParameterType(param.ParameterType)
                ));
            invoke.Body = new MethodBody(invoke);
            del.Methods.Add(invoke);

            var invokeBegin = new MethodDefinition(
                "BeginInvoke",
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                rdata.IAsyncResultType
            )
            {
                ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed,
                HasThis = true
            };
            foreach (var param in invoke.Parameters)
                invokeBegin.Parameters.Add(new ParameterDefinition(param.Name, param.Attributes, param.ParameterType));
            invokeBegin.Parameters.Add(new ParameterDefinition("callback", ParameterAttributes.None,
                rdata.AsyncCallbackType));
            invokeBegin.Parameters.Add(new ParameterDefinition(null, ParameterAttributes.None, typeSystem.Object));
            invokeBegin.Body = new MethodBody(invokeBegin);
            del.Methods.Add(invokeBegin);

            var invokeEnd = new MethodDefinition(
                "EndInvoke",
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                typeSystem.Object
            )
            {
                ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed,
                HasThis = true
            };
            invokeEnd.Parameters.Add(new ParameterDefinition("result", ParameterAttributes.None, rdata.IAsyncResultType));
            invokeEnd.Body = new MethodBody(invokeEnd);
            del.Methods.Add(invokeEnd);

            return del;
        }

    }
}
