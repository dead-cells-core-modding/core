using Hashlink.Marshaling;

namespace Hashlink.Proxy.Objects
{
    public unsafe class HashlinkNETExceptionObj( HashlinkObjPtr ptr ) : HashlinkTypedObj<HL_vdynamic>(ptr)
    {
        public Exception? Exception
        {
            get; set;
        }
        public HashlinkNETExceptionObj( Exception ex ) : this(HashlinkObjPtr.Get(
            hl_alloc_obj(NETExcepetionError.ErrorType)
            ))
        {
            MarkStateful();
            Exception = ex;
        }

        public override string ToString()
        {
            return Exception == null ? "" : $"[.NET Exception] {Exception}";
        }
    }
}
