using ModCore.Events.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                receivers.Add((IEventReceiver) v!);
            }
        }

        private static void EventSystem_OnRemoveReceiver(IEventReceiver obj)
        {
            if(obj is TEvent)
            {
                receivers.Remove(obj);
            }
        }

        private static void EventSystem_OnAddReceiver(IEventReceiver obj)
        {
            if(obj is TEvent)
            {
                receivers.Add(obj);
            }
        }

        public readonly static EventReceiverList receivers = [];
    }
}
