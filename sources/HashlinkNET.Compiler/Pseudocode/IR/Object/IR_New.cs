using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Data.Interfaces;
using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode.IR.Object
{
    class IR_New(
        TypeReference type,
        params IRResult[] ctorArgs
        ) : IRBase(ctorArgs)
    {
        public TypeReference type = type;
        public readonly IRResult[] ctorArgs = ctorArgs;

        private static MethodReference FindCtor( EmitContext ctx, IDataContainer container,
            TypeReference type, int argCount )
        {
            if (container.TryGetData<ObjClassData>(type, out var od) &&
                od.ConstructArgs != null &&
                od.ConstructArgs.Parameters.Count == argCount)
            {
                return od.ConstructArgs;
            }
            if (container.TryGetData<IConstructable>(type, out var c) &&
                c.Construct.Parameters.Count == argCount)
            {
                return c.Construct;
            }
            if (argCount == 0)
            {
                // Runtime-provided / external types (e.g. ExpandoObject) only
                // need a parameterless constructor reference.
                return new MethodReference(".ctor", ctx.TypeSystem.Void, type)
                {
                    HasThis = true
                };
            }
            throw new InvalidOperationException(
                $"Constructor with {argCount} parameter(s) not found for '{type.FullName}'.");
        }

        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            foreach (var v in ctorArgs)
            {
                v.Emit(ctx, true);
            }

            var ctor = FindCtor(ctx, container, type, ctorArgs.Length);
            il.Emit(OpCodes.Newobj, ctor);
            return type;
        }
    }
}
