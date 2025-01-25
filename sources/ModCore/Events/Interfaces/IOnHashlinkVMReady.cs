namespace ModCore.Events.Interfaces
{
    [Event(true)]
    public interface IOnHashlinkVMReady
    {
        public void OnHashlinkVMReady();
    }
}
