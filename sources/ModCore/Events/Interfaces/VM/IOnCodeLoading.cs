namespace ModCore.Events.Interfaces.VM
{
    [Event(true)]
    public interface IOnCodeLoading
    {
        public void OnCodeLoading( ref ReadOnlySpan<byte> data );
    }
}
