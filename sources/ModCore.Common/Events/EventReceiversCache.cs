using ModCore.Events.Collections;

namespace ModCore.Events
{
    internal static class EventReceiversCache<TEvent>
    {
        private static volatile int callCount = 0;
        private static volatile int version = 0;
        static EventReceiversCache()
        {
            EventSystem.OnAddReceiver += EventSystem_OnAddReceiver;
            EventSystem.OnRemoveReceiver += EventSystem_OnRemoveReceiver;

            foreach (var v in EventSystem.FindReceivers<TEvent>())
            {
                receivers.Add((IEventReceiver)v!);
            }
        }

        private static void EventSystem_OnRemoveReceiver( IEventReceiver obj )
        {
            if (obj is TEvent)
            {
                receivers.Remove(obj);
            }
        }

        private static void EventSystem_OnAddReceiver( IEventReceiver obj )
        {
            if (obj is TEvent)
            {
                receivers.Add(obj);
            }
        }

        public static IEnumerable<IEventReceiver> GetReceivers()
        {
            _RE_TRY:

            if (version != receivers.Version)
            {
                fastEventReceivers = null;
            }

            if (fastEventReceivers == null)
            {
                Interlocked.Increment(ref callCount);

                if (callCount > 100)
                {
                    lock (updateFastLock)
                    {
                        if (fastEventReceivers == null)
                        {
                            callCount = 0;
                            version = receivers.Version;
                            fastEventReceivers = [.. receivers];

                            goto _RE_TRY;
                        }
                    }
                }
            }

            return (IEnumerable<IEventReceiver>?)fastEventReceivers ?? receivers;
        }

        private readonly static Lock updateFastLock = new();
        private static IEventReceiver[]? fastEventReceivers;
        private static readonly EventReceiverList receivers = [];
    }
}
