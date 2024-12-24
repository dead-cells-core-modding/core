using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Track
{
    public class HLStackFrame : StackFrame
    {
        public int FileLine { get; set; }
        public string? FileName { get; set; }
        public string? FuncName { get; set; }
        public nint Pointer { get; set; }
        public override string ToString()
        {
            return $"{FuncName} in file:line:ptr {FileName}:{FileLine}:{Pointer:x}";
        }
        public override int GetILOffset()
        {
            return 0;
        }
        public override int GetFileColumnNumber()
        {
            return 0;
        }
        public override int GetFileLineNumber()
        {
            return FileLine;
        }
        public override string? GetFileName()
        {
            return FileName;
        }
        public override int GetNativeOffset()
        {
            return 0;
        }
        public override MethodBase? GetMethod()
        {
            return null;
        }
    }
}
