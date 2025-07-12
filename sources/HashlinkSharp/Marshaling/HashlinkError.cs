namespace Hashlink.Marshaling
{
    public unsafe class HashlinkError : Exception
    {
        public string stackTrace = "";
        public nint Error
        {
            get;
        }
        public HashlinkError( nint err ) : base(
            $"Uncaught hashlink exception.{(err == 0 ? "<null>" : new string(hl_to_string((HL_vdynamic*)err)))}"
            )
        {
            Error = err;
        }

        //public override string? StackTrace
        //{
        ///    get => stackTrace;
        //}
    }
}
