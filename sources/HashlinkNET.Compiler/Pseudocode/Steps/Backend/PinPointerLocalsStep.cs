using HashlinkNET.Compiler.Pseudocode.Data;
using HashlinkNET.Compiler.Steps;
using HaxeProxy.Runtime;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Pseudocode.Steps.Backend
{
    /// <summary>
    /// Marks locals that carry references or raw pointers as pinned so that
    /// the GC will not relocate the referenced storage while the translated
    /// code manipulates it through pointers.
    /// </summary>
    internal class PinPointerLocalsStep : CompileStep
    {
        public override void Execute( IDataContainer container )
        {
            var gdata = container.GetGlobalData<FuncEmitGlobalData>();
            var md = gdata.Definition;
            var refTypeName = typeof(Ref<>).FullName;

            foreach (var variable in md.Body.Variables)
            {
                var type = variable.VariableType;
                if (type.IsPinned)
                {
                    continue;
                }
                if (type.IsByReference ||
                    type.IsPointer ||
                    type.MetadataType is MetadataType.IntPtr or MetadataType.UIntPtr ||
                    (type is GenericInstanceType git &&
                     git.ElementType.FullName == refTypeName))
                {
                    variable.VariableType = new PinnedType(type);
                }
            }
        }
    }
}
