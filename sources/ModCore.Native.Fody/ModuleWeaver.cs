using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ModCore.Native.Fody
{
    public class ModuleWeaver : BaseModuleWeaver
    {
        public override void Execute()
        {
            var nt = ModuleDefinition.GetType("ModCore.Native.Native");
            var getSym = nt.Methods.First(x => x.Name == "GetLibhlSymbolEx");
            var hn = ModuleDefinition.GetType("Hashlink.HashlinkNative");

            foreach (var m in hn.Methods)
            {
                var dia = m.PInvokeInfo;
                if (dia == null)
                {
                    continue;
                }

                m.PInvokeInfo = null;
                m.IsPInvokeImpl = false;
                m.IsIL = true;

                var body = new MethodBody(m);

       
                m.Body = body;
                var il = body.GetILProcessor();


                var callInfo = new CallSite(m.ReturnType)
                {
                    CallingConvention = MethodCallingConvention.Unmanaged
                };

                var fpt = new FunctionPointerType()
                {
                    ReturnType = callInfo.ReturnType,
                    CallingConvention = callInfo.CallingConvention,
                };


                foreach (var p in m.Parameters)
                {
                    il.Emit(OpCodes.Ldarg, p);
                    callInfo.Parameters.Add(new(p.ParameterType));
                    fpt.Parameters.Add(new(p.ParameterType));
                }

                var cf = new FieldDefinition("cached_" + m.Name, FieldAttributes.Private | FieldAttributes.Static,
                    fpt);
                hn.Fields.Add(cf);

                var symName = dia.EntryPoint;

                if (string.IsNullOrEmpty(symName))
                {
                    symName = m.Name;
                }

                il.Emit(OpCodes.Ldstr, symName);
                il.Emit(OpCodes.Ldsflda, cf);
                il.Emit(OpCodes.Call, getSym);

                il.Emit(OpCodes.Calli, callInfo);

                il.Emit(OpCodes.Ret);
            }
        }

        public override IEnumerable<string> GetAssembliesForScanning()
        {
            return [];
        }
    }
}
