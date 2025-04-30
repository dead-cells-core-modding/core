using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Data.Interfaces;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Steps.Class
{
    internal class GenerateClassGlobalPropStep : ForeachHlTypeCompileStep
    {
        public override bool Filter( HlType type )
        {
            return type.Kind == HlTypeKind.Obj ||
                type.Kind == HlTypeKind.Struct ||
                type.Kind == HlTypeKind.Enum;
        }
        public override void Execute( IDataContainer container,
            HlCode code, GlobalData gdata,
            RuntimeImports rdata, HlType type )
        {
            var gv = container.GetData<IGlobalValueSetter>(type);
            var td = container.GetData<ITypeDefinitionValue>(type).TypeDef;

            int globalId;
            if (type is HlTypeWithObj hlobj)
            {
                globalId = hlobj.Obj.GlobalValue - 1;
            }
            else if (type is HlTypeWithEnum hlenum)
            {
                globalId = hlenum.Enum.GlobalValue - 1;
            }
            else
            {
                throw new InvalidOperationException();
            }
            if (globalId >= gdata.Code.Globals.Count || globalId < 0)
            {
                return;
            }
            var gr = gdata.Code.Globals[globalId];
            var cinfo = container.GetData<ObjClassData>(gr.Value);
            var ct = cinfo.TypeDef;

            var gm = new MethodDefinition("get_Class", MethodAttributes.Public | MethodAttributes.SpecialName
                    | MethodAttributes.Static, ct);
            var mp = new PropertyDefinition("Class", PropertyAttributes.None, ct)
            {
                GetMethod = gm
            };
            td.Methods.Add(gm);
            td.Properties.Add(mp);

            gv.GlobalClassProp = mp;
            gv.GlobalClassType = ct;
            gv.GlobalHlType = (HlTypeWithObj) gr.Value;

            var ct_ctor = (MethodDefinition)cinfo.Construct;
            ct_ctor.IsAssembly = true;
            var cf = new FieldDefinition("cachedClassValue", FieldAttributes.Private | FieldAttributes.Static, ct);
            td.Fields.Add(cf);

            {
                var gmil = gm.Body.GetILProcessor();
                gmil.Emit(OpCodes.Ldc_I4, globalId);
                gmil.Emit(OpCodes.Ldsflda, cf);
                gmil.Emit(OpCodes.Call, rdata.hGetGlobal);
                gmil.Emit(OpCodes.Ret);
            }

            if (cinfo.GlobalClassType == null)
            {
                cinfo.GlobalClassProp = mp;
                cinfo.GlobalClassType = ct;
            }
            gv.GlobalClassField = cf;
        }
    }
}
