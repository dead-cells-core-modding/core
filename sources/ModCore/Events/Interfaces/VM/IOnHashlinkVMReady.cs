namespace ModCore.Events.Interfaces.VM
{
    /// <summary>
    /// An event triggered when the hashlink vm is ready
    /// </summary>
    [Event(true)]
    public interface IOnHashlinkVMReady
    {
        /// <summary>
        /// 
        /// </summary>
        public void OnHashlinkVMReady();
    }
}
