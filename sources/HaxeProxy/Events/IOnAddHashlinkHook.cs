using Hashlink.Reflection.Members;
using ModCore.Events;

namespace HaxeProxy.Events
{
    [Event]
    internal interface IOnAddHashlinkHook
    {
        public record class Data( HashlinkFunction Function, Delegate Target );
        public void OnAddHashlinkHook( Data data );
    }
}
