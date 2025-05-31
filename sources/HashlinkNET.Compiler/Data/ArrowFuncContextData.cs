using Mono.Cecil;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Data
{
    internal class ArrowFuncContextData :
        EnumClassData
    {
        public ConcurrentBag<FuncData> Methods
        {
            get; set;
        } = [];
    }
}
