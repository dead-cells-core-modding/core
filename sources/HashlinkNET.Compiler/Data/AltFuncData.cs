using Mono.Cecil;

namespace HashlinkNET.Compiler.Data
{
    /// <summary>
    /// Maps a translated (main) method to its Ref&lt;T&gt;-based overload /
    /// wrapper, which is signature-compatible with Haxe function delegates.
    /// </summary>
    internal class AltFuncData( MethodReference alt )
    {
        public MethodReference Alt {
            get;
        } = alt;
    }
}
