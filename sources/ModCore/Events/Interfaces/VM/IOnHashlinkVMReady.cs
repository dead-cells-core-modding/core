namespace ModCore.Events.Interfaces.VM
{
    /// <summary>
    /// An event triggered when the hashlink vm is ready
    /// </summary>
    [Event(true)]
    public interface IOnHashlinkVMReady
    {
        /// <summary>
        /// Raised when the Hashlink VM is ready.
        /// </summary>
        public void OnHashlinkVMReady();
    }
}
