namespace ModCore.Events.Interfaces.VM
{
    /// <summary>
    /// An event raised while game code is being loaded.
    /// </summary>
    [Event(true)]
    public interface IOnCodeLoading
    {
        /// <summary>
        /// Raised while game code is being loaded, allowing the code to be modified.
        /// </summary>
        /// <param name="data">The code bytes being loaded.</param>
        public void OnCodeLoading( ref ReadOnlySpan<byte> data );
    }
}
