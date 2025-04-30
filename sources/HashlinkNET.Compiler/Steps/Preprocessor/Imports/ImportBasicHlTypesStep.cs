using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Steps.Preprocessor.Imports
{
    internal class ImportBasicHlTypesStep : ForeachHlTypeCompileStep
    {
        public override void Execute( IDataContainer data, HlCode code, GlobalData gdata,
            RuntimeImports rdata, HlType t )
        {
            var ts = gdata.Module.TypeSystem;
            var tr = t.Kind switch
            {
                HlTypeKind.Void => ts.Void,
                HlTypeKind.UI8 => ts.Byte,
                HlTypeKind.UI16 => ts.UInt16,
                HlTypeKind.I32 => ts.Int32,
                HlTypeKind.I64 => ts.Int64,
                HlTypeKind.F32 => ts.Single,
                HlTypeKind.F64 => ts.Double,
                HlTypeKind.Bool => ts.Boolean,
                HlTypeKind.Bytes => rdata.bytesType,
                HlTypeKind.Array => rdata.nativeArray,
                HlTypeKind.Dyn => ts.Object,
                HlTypeKind.DynObj => rdata.dynType,
                HlTypeKind.Abstract => ts.IntPtr,
                HlTypeKind.Type => gdata.Module.ImportReference(typeof(Type)),
                _ => null
            };
            if (tr != null)
            {
                data.AddData(t, tr);
                return;
            }

        
        }
    }
}
