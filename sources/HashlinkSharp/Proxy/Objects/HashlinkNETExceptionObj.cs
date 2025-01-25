using Hashlink.Marshaling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Proxy.Objects
{
    public unsafe class HashlinkNETExceptionObj(HashlinkObjPtr ptr) : HashlinkTypedObj<HL_vdynamic>(ptr)
    {
        public Exception? Exception { get; set; }
        public HashlinkNETExceptionObj(Exception ex) : this(HashlinkObjPtr.GetUnsafe(NETExcepetionError.ErrorType))
        {
            Exception = ex;
        }

        public override string ToString()
        {
            if(Exception == null)
            {
                return "";
            }
            return "[.NET Exception]" + Exception.Message;
        }
    }
}
