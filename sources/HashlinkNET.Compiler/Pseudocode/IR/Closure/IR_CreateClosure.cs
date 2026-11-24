using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Data.Interfaces;
using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode.IR.Closure
{
    internal class IR_CreateClosure(
        MethodReference method,
        TypeReference type,
        bool isVirt,
        IRResult? self
        ) : IRBaseWithSideEffects(self)
    {
        protected override TypeReference? Emit( EmitContext ctx, IDataContainer container, ILProcessor il )
        {
            TypeReference? selfType = null;
            if (self != null)
            {
                selfType = self.Emit(ctx, true);
                if (selfType != null &&
                    selfType.IsValueType &&
                    !selfType.IsByReference)
                {
                    il.Emit(OpCodes.Box, selfType);
                    selfType = ctx.TypeSystem.Object;
                }
            }
            else
            {
                il.Emit(OpCodes.Ldnull);
            }

            // Translated methods use 'ref T', but delegates use Ref<T>.
            // Bind overloads/wrappers with the matching signature instead.
            var target = method;
            if (method.Parameters.Any(p => p.ParameterType.IsByReference) &&
                container.TryGetData<AltFuncData>(method, out var altData))
            {
                target = altData.Alt;
            }

            if (isVirt && target.HasThis && selfType != null && target.DeclaringType == selfType)
            {
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Ldvirtftn, target);
            }
            else
            {
                il.Emit(OpCodes.Ldftn, target);
            }
            il.Emit(OpCodes.Newobj, container.GetData<IConstructable>(type).Construct);
            return type;
        }
    }
}
