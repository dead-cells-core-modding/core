using dc;
using Hashlink.Marshaling;
using Hashlink.Reflection.Members;
using Haxe2CSharp;
using ModCore.Modules.Internals;
using ModCore.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace TestRunner
{
    public class Haxe2CS
    {
        [Fact]
        public void Compile_Funcs()
        {
            var compiler = new HaxeCompiler("TestRunner.TestCode.Funcs", HashlinkMarshal.Module,
                typeof(Boot).Assembly,
                HaxeProxyGenerator.Instance.Code!);

            int[] funcs = [
                21909,
                21915,
                21892,
                21894,
                21883,
                17127
                ];

            foreach(var v in funcs)
            {
                compiler.Compile((HashlinkFunction) HashlinkMarshal.Module.GetFunctionByFIndex(v));
            }

            compiler.Assembly.Write(FolderInfo.Cache.GetFilePath("haxe2cs_test1.dll"));
        }
    }
}
