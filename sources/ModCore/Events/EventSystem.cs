using ModCore.Events.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Events
{
    public static class EventSystem
    {
        private static readonly EventReceiverList eventReceivers = [];

        public static void AddReceiver(IEventReceiver receiver)
        {
            eventReceivers.Add(receiver);
        }
        public static void RemoveReceiver(IEventReceiver receiver)
        {
            eventReceivers.Remove(receiver);
        }

        public static T? FindReceiver<T>() where T : IEventReceiver
        {
            return eventReceivers.OfType<T>().FirstOrDefault();
        }
        public static IEnumerable<T> FindReceivers<T>() where T : IEventReceiver
        {
            return eventReceivers.OfType<T>();
        }

        public static void BroadcastEvent<TEvent>()
        {
            foreach (var module in eventReceivers)
            {
                if (module is TEvent ev)
                {
                    EventCaller<TEvent>.Invoke(ev);
                }
            }
        }
        public static void BroadcastEvent<TEvent, TArg>(TArg arg)
        {
            foreach (var module in eventReceivers)
            {
                if (module is TEvent ev)
                {
                    EventCaller<TEvent>.Invoke(ev, ref arg);
                }
            }
        }

    }
}
