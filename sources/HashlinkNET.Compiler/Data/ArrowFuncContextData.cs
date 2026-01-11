using System.Collections.Concurrent;

namespace HashlinkNET.Compiler.Data
{
    internal class ArrowFuncContextData :
        EnumClassData
    {
        public FuncData? DirectParent
        {
            get; set;
        }
        public ConcurrentBag<FuncData> Methods
        {
            get; set;
        } = [];
    }
}
