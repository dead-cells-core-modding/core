using Hashlink.Marshaling;

namespace Hashlink.Proxy.Objects
{
    public unsafe class HashlinkNETExceptionObj( HashlinkObjPtr ptr ) : HashlinkTypedObj<HL_vdynamic>(ptr)
    {
        public Exception? Exception
        {
            get; set;
        }
        public HashlinkNETExceptionObj( Exception ex ) : this(HashlinkObjPtr.GetUnsafe(
            hl_alloc_obj(NETExcepetionError.ErrorType)
            ))
        {
            Exception = ex;
        }

        public override string ToString()
        {
            CheckValidity();
            return Exception == null ? "" : $"[.NET Exception] {Exception}";
        }
    }
}
