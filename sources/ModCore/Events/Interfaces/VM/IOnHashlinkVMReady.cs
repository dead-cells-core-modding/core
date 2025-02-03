namespace ModCore.Events.Interfaces.VM
{
    [Event(true)]
    public interface IOnHashlinkVMReady
    {
        public void OnHashlinkVMReady();
    }
}
