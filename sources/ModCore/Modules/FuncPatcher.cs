using Hashlink.Marshaling;
using Hashlink.Patch;
using Hashlink.Reflection.Members;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ModCore.Modules
{
    [CoreModule(CoreModuleAttribute.CoreModuleKind.Normal)]
    public unsafe class FuncPatcher : CoreModule<FuncPatcher>
    {
        public delegate void PatcherCallback( HlFunctionDefinition definition );

        private readonly HashSet<HashlinkFunction> patched = [];

        public void CreatePatch( string typeName, string funcName, PatcherCallback callback )
        {
            CreatePatch(HashlinkMarshal.FindFunction(typeName, funcName), callback);
        }
        public void CreatePatch( HashlinkFunction function, PatcherCallback callback )
        {
            if (!patched.Add(function))
            {
                throw new InvalidOperationException();
            }
            var def = new HlFunctionDefinition();
            def.ReadFrom( function );
            callback( def );
            def.VerifyOpCodes();
            var ptr = def.Compile();

            NativeHooks.Instance.CreateHook(function.EntryPointer + HashlinkFunction.FS_OFFSET_REAL_ENTRY, ptr, true).Enable();
        }
    }
}
