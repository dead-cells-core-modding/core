namespace ModCore.Events.Interfaces
{
    [Event(true)]
    public interface IOnCodeLoading
    {
        public void OnCodeLoading( ref Span<byte> data );
    }
}
