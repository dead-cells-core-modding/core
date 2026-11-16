using HashlinkNET.Compiler.Data;
using Mono.Cecil;

namespace HashlinkNET.Compiler.Utils
{
    internal static class HashlinkMethodMarker
    {
        public const string ShouldUseNativeAttributeName =
            "HaxeProxy.Runtime.Internals.HashlinkShouldUseNativeAttribute";

        public static void MarkShouldUseNative( IDataContainer container, MethodDefinition md )
        {
            if (md.CustomAttributes.Any(a =>
                a.AttributeType.FullName == ShouldUseNativeAttributeName))
            {
                return;
            }

            var rdata = container.GetGlobalData<RuntimeImports>();
            md.CustomAttributes.Add(new CustomAttribute(rdata.attrShouldUseNative));
        }
    }
}
