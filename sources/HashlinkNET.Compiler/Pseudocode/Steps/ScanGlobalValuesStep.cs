using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Steps;
using System.Diagnostics;

namespace HashlinkNET.Compiler.Pseudocode.Steps
{
    internal class ScanGlobalValuesStep : CompileStep
    {
        public override void Execute( IDataContainer container )
        {
            var gdata = container.GetGlobalData<GlobalData>();

            var func = gdata.Code.GetFunctionById(gdata.Code.Entrypoint);

            Debug.Assert(func != null);

            for (int i = 0; i < func.Opcodes.Length; i++)
            {
                var opc = func.Opcodes[i];
                if (opc.Kind == HlOpcodeKind.SetGlobal)
                {
                    var gid = opc.Parameters[0];
                    var gt = gdata.Code.Globals[gid];
                    if (gt.Value is HlTypeWithEnum te)
                    {
                        var tei = container.GetData<EnumClassData>(te);
                        tei.GlobalValueIndex.Add(gid);
                    }
                }
            }
        }
    }
}
