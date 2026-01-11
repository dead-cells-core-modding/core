using ModCore.Events;

namespace Hashlink.Events.Interfaces
{
    [Event]
    public interface IOnRegisterHashlinkThread
    {
        void OnRegisterHashlinkThread();
    }
}
