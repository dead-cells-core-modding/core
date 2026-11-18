using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Diagnostics;

namespace HashlinkNET.Compiler.Pseudocode.IR.Call
{
    class IR_CallClosure(
        MethodReference? invokeMethod,
        IRResult src,
        params IRResult[] args
        ) : IRBase([src, .. args])
    {
        public MethodReference? invokeMethod = invokeMethod;
        public readonly IRResult src = src;
        public readonly IRResult[] args = args;
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            src.Emit(ctx, true);
            if (invokeMethod != null)
            {
                for (var i = 0; i < args.Length; i++)
                {
                    var actual = args[i].Emit(ctx, true)!;
                    var expected = invokeMethod.Parameters[i].ParameterType;
                    if (expected is GenericParameter gp &&
                        gp.Type == GenericParameterType.Type &&
                        invokeMethod.DeclaringType is GenericInstanceType dgi &&
                        gp.Position < dgi.GenericArguments.Count)
                    {
                        expected = dgi.GenericArguments[gp.Position];
                    }

                    Debug.Assert(actual != null);
                    Debug.Assert(expected != null);

                    if (actual.FullName != expected.FullName)
                    {
                        new IR_Cast(new IR_LoadFromStack(actual), expected, false).Emit(ctx, true);
                    }
                }
                il.Emit(OpCodes.Callvirt, invokeMethod);
                var rt = invokeMethod.ReturnType;
                if (rt is GenericParameter gpr &&
                    invokeMethod.DeclaringType is GenericInstanceType dgi2)
                {
                    Debug.Assert(gpr.Type == GenericParameterType.Type);
                    rt = dgi2.GenericArguments[gpr.Position];
                }

                return rt;
            }
            else
            {
                var im = container.GetData<RuntimeImports>(container.Parent!).delegateDynInvokeMethod;

                il.Emit(OpCodes.Ldc_I4, args.Length);
                il.Emit(OpCodes.Newarr, ctx.TypeSystem.Object);

                for (var i = 0; i < args.Length; i++)
                {
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Ldc_I4, i);
                    var argType = args[i].Emit(ctx, true)!;
                    if (argType.IsByReference)
                    {
                        var element = ((ByReferenceType)argType).ElementType;
                        il.Emit(OpCodes.Ldobj, element);
                        if (element.IsValueType && !element.IsGenericParameter)
                        {
                            il.Emit(OpCodes.Box, element);
                        }
                    }
                    else if (argType.IsValueType && !argType.IsGenericParameter)
                    {
                        il.Emit(OpCodes.Box, argType);
                    }
                    il.Emit(OpCodes.Stelem_Any, ctx.TypeSystem.Object);
                }
                il.Emit(OpCodes.Callvirt, im);
                return ctx.TypeSystem.Object;
            }
        }
    }
}

