using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using K4os.Hash.xxHash;
using System.Security.Cryptography;
using System.Text;

namespace HashlinkNET.Compiler.Steps.Func.ArrowFunc
{
    internal class FixArrowFuncContextNameStep : ForeachHlTypeCompileStep
    {
        public override bool Filter( HlType type )
        {
            return type.Kind == HlTypeKind.Enum;
        }
        public override void Execute( IDataContainer container, HlCode code,
            GlobalData gdata, RuntimeImports rdata, HlType type )
        {
            if (!container.TryGetData<ArrowFuncContextData>(type, out var data))
            {
                return;
            }
            var td = data.TypeDef;
            if (data.DirectParent != null)
            {
                var l = new List<FuncData>(data.Methods);
                l.Sort(( a, b ) => a.Definition.Name.CompareTo(b.Definition.Name));
                td.Name = data.DirectParent.Definition.Name + "Context_" + XXH32.DigestOf(
                    Encoding.UTF8.GetBytes(l[0].Definition.Name)
                    );
            }

        }
    }
}
