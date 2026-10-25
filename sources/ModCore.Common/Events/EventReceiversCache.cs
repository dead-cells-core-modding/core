using ModCore.Events.Collections;
using System.Diagnostics;

namespace ModCore.Events
{
    internal static class EventReceiversCache<TEvent>
    {
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
                cachedReceiver = null;
            }
        }

        private static void EventSystem_OnAddReceiver( IEventReceiver obj )
        {
            if (obj is TEvent)
            {
                receivers.Add(obj);
                cachedReceiver = null;
            }
        }

        public static IEnumerable<IEventReceiver> GetReceivers()
        {
            if (cachedReceiver == null ||
                cachedReceiver.Length != receivers.Count)
            {
                cachedReceiver = [.. receivers];
            }

            //Debug.Assert(cachedReceiver.Length == receivers.Count);

            return cachedReceiver;
        }
        private static IEventReceiver[]? cachedReceiver;
        private static readonly EventReceiverList receivers = [];
    }
}
