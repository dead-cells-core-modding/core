using Hashlink.Reflection.Members;
using ModCore.Events;

namespace HaxeProxy.Events
{
    [Event]
    internal interface IOnRemoveHashlinkHook
    {
        public record class Data(HashlinkFunction Function, Delegate Target);
        public void OnRemoveHashlinkHook( Data data );
    }
}
