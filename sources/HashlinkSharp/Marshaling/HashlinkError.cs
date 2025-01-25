namespace Hashlink.Marshaling
{
    public unsafe class HashlinkError : Exception
    {
        private readonly string stackTrace;
        public nint Error
        {
            get;
        }
        public HashlinkError( nint err, string stack ) : base(
            $"Uncaught hashlink exception.{(err == 0 ? "<null>" : new string(hl_to_string((HL_vdynamic*)err)))}"
            )
        {
            stackTrace = stack;
            Error = err;
        }

        public override string? StackTrace
        {
            get => stackTrace;
        }
    }
}
