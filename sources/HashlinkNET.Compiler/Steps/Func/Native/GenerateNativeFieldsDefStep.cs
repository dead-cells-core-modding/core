
using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HashlinkNET.Compiler.Steps.Func.Native
{
    internal class GenerateNativeFieldsDefStep : CompileStep
    {
        public override void Execute( IDataContainer container )
        {
            var gdata = container.GetGlobalData<GlobalData>();
            var rdata = container.GetGlobalData<RuntimeImports>();
            var ncls = container.GetGlobalData<NativeImplClasses>();

            foreach (var v in gdata.Code.Natives)
            {
                var td = ncls.NativeImplClass[v.Lib];
                var pd = td.Properties.FirstOrDefault(x => x.Name == v.Name);
                if (pd != null)
                {
                    container.AddData(v, pd);
                    continue;
                }

                var pt = container.GetTypeRef(v.Type.Value);
                pd = new PropertyDefinition(v.Name, PropertyAttributes.None,
                    pt)
                {
                    GetMethod = new("get_" + v.Name, MethodAttributes.Public | MethodAttributes.Static, pt),
                };
                container.AddDataEach(v, pd);
                td.Methods.Add(pd.GetMethod);
                td.Properties.Add(pd);

                if (!gdata.Config.GeneratePseudocode)
                {
                    var md = pd.GetMethod;
                    md.Body = new(md);
                    var il = md.Body.GetILProcessor();

                    var cf = new FieldDefinition("cache_" + v.Name, FieldAttributes.Static | FieldAttributes.Private, pt);

                    td.Fields.Add(cf);

                    il.Emit(OpCodes.Ldc_I4, v.NativeIndex);
                    il.Emit(OpCodes.Ldsflda, cf);
                    il.Emit(OpCodes.Call, rdata.hGetNativeCall.MakeInstance(pt));
                    il.Emit(OpCodes.Ret);
                }
            }
        }
    }
}
