using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;

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
                    var argType = args[i].Emit(ctx, true)!;
                    var paramType = i < invokeMethod.Parameters.Count
                        ? invokeMethod.Parameters[i].ParameterType
                        : null;

                    // Delegates (notably native ones) still take Ref<T> even
                    // though translated methods use 'ref T'.
                    if (paramType is GenericInstanceType pgi &&
                        pgi.ElementType.FullName == ctx.RuntimeImports.refType.FullName &&
                        argType.IsByReference)
                    {
                        il.Emit(OpCodes.Call, ctx.RuntimeImports.phMakeRef.MakeInstance(pgi.GenericArguments[0]));
                    }
                }
                il.Emit(OpCodes.Callvirt, invokeMethod);
                return invokeMethod.ReturnType;
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
