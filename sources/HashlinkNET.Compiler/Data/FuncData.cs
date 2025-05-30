using Mono.Cecil;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Data
{
    internal class FuncData
    {
        public ObjClassData? DeclaringClass
        {
            get; set;
        }
        public required MethodDefinition Definition
        {
            get; set;
        }
        public List<(FuncData, int)> UsedBy
        {
            get; set;
        } = [];
    }
}
