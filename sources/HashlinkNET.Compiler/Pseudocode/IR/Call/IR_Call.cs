using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode.IR.Call
{
    class IR_Call(
        MethodReference method,
        bool virt,
        params IRResult[] args
        ) : IRBase(args)
    {
        public MethodReference method = method;
        public readonly IRResult[] args = args;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {

            for (var i = 0; i < args.Length; i++)
            {
                var actual = args[i].Emit(ctx, true);

                if (method.HasThis && i == 0 && actual != null &&
                    !actual.IsValueType && !actual.IsByReference &&
                    !method.DeclaringType.IsValueType &&
                    !method.DeclaringType.IsGenericParameter &&
                    !method.DeclaringType.IsByReference &&
                    method.DeclaringType.FullName != "System.Object")
                {
                    il.Emit(OpCodes.Castclass, method.DeclaringType);
                }

                TypeReference? expected = null;
                if (method.HasThis)
                {
                    if (i == 0)
                    {
                        expected = method.DeclaringType;
                    }
                    else if (i - 1 < method.Parameters.Count)
                    {
                        expected = method.Parameters[i - 1].ParameterType;
                    }
                }
                else if (i < method.Parameters.Count)
                {
                    expected = method.Parameters[i].ParameterType;
                }

                if (expected is GenericParameter gp &&
                    gp.Type == GenericParameterType.Type &&
                    method.DeclaringType is GenericInstanceType dgi &&
                    gp.Position < dgi.GenericArguments.Count)
                {
                    expected = dgi.GenericArguments[gp.Position];
                }

                if (expected is GenericInstanceType pgi &&
                    pgi.ElementType.FullName == ctx.RuntimeImports.refType.FullName &&
                    actual.IsByReference)
                {
                    il.Emit(OpCodes.Call,
                        ctx.RuntimeImports.phMakeRef.MakeInstance(pgi.GenericArguments[0]));
                }
                else
                {
                    il.EmitNullableConversion(container, expected, actual);
                    if (expected != null && actual != null &&
                        !expected.IsValueType && !actual.IsValueType &&
                        !actual.IsByReference && !expected.IsByReference &&
                        expected.FullName != "System.Object" &&
                        actual.FullName != expected.FullName)
                    {
                        // Haxe switch/type narrowing does not change the CLR
                        // static type; insert the required downcast.
                        il.Emit(OpCodes.Castclass, expected);
                    }
                    if (expected?.FullName == "System.Object")
                    {
                        var boxType = actual;
                        if (boxType.IsByReference)
                        {
                            boxType = ((ByReferenceType)boxType).ElementType;
                            il.Emit(OpCodes.Ldobj, boxType);
                        }
                        if (boxType.IsValueType && !boxType.IsGenericParameter)
                        {
                            il.Emit(OpCodes.Box, boxType);
                        }
                    }
                }
            }

            il.Emit(virt && method.HasThis ? OpCodes.Callvirt : OpCodes.Call, method);
            return method.ReturnType;
        }
    }
}
