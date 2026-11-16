using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Data.Interfaces;
using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Diagnostics;

namespace HashlinkNET.Compiler.Pseudocode.IR.Object
{
    class IR_New(
        TypeReference type,
        params IRResult[] ctorArgs
        ) : IRBase(ctorArgs)
    {
        public TypeReference type = type;
        public readonly IRResult[] ctorArgs = ctorArgs;

        private static MethodReference? FindCtor( EmitContext ctx, IDataContainer container,
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
                try
                {
                    lock (type)
                    {
                        var def = type.Resolve();
                        if (def != null)
                        {
                            foreach (var m in def.Methods)
                            {
                                if (m.IsConstructor && !m.IsStatic && m.Parameters.Count == 0)
                                {
                                    return m;
                                }
                            }
                        }
                    }
                }
                catch
                {
                }
                // No real parameterless constructor. Haxe allocates the
                // object separately from __inst_construct__, so create an
                // uninitialized instance instead of emitting an invalid
                // newobj against a non-existent .ctor.
                return null;
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
            if (ctor != null)
            {
                Debug.Assert(ctor.DeclaringType is not TypeDefinition td || !td.IsAbstract);

                il.Emit(OpCodes.Newobj, il.Body.Method.Module.ImportReference(ctor));
            }
            else
            {
                il.Emit(OpCodes.Call,
                    ctx.RuntimeImports.phCreateEmptyInstance.MakeInstance(type));
            }
            return type;
        }
    }
}
