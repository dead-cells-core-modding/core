using ModCore.Events.Collections;
using Serilog;
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
        private static ILogger Logger { get; } = Log.Logger.ForContext("SourceContext", "EventSystem");
        [Flags]
        public enum ExceptionHandingFlags
        {
            Default = Continue ,

            Continue = 1,
            NoThrow = 2,
            Quiet = 4
        }
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

        public static void BroadcastEvent<TEvent>(ExceptionHandingFlags flags = ExceptionHandingFlags.Default)
        {
            BroadcastEvent<TEvent, int>(0, flags);
        }
        public static void BroadcastEvent<TEvent, TArg>(TArg arg, ExceptionHandingFlags flags = ExceptionHandingFlags.Default)
        {
            BroadcastEvent<TEvent, TArg>(ref arg, flags);
        }
        public static void BroadcastEvent<TEvent, TArg>(ref TArg arg, ExceptionHandingFlags flags = ExceptionHandingFlags.Default) 
            where TArg : allows ref struct
        {
            if(EventCaller<TEvent>.IsCallOnce)
            {
                if(EventCaller<TEvent>.IsCalled)
                {
                    throw new InvalidOperationException("An event that should only be called once was called multiple times");
                }
                Logger.Information("Broadcast Global Event: {Name}", typeof(TEvent).Name); 
            }
            List<Exception>? exceptions = null;
            foreach (var module in eventReceivers)
            {
                if (module is TEvent ev)
                {
                    try
                    {
                        EventCaller<TEvent>.Invoke(ev, ref arg);
                    }
                    catch (Exception ex)
                    {
                        if (!flags.HasFlag(ExceptionHandingFlags.Quiet))
                        {
                            Logger.Error(ex, "An exception occurred when executing event");
                        }
                        if (flags.HasFlag(ExceptionHandingFlags.NoThrow))
                        {
                            continue;
                        }
                        if (!flags.HasFlag(ExceptionHandingFlags.Continue))
                        {
                            throw;
                        }
                        exceptions ??= [];
                        exceptions.Add(ex);
                    }
                }
            }
            if (exceptions != null)
            {
                throw new AggregateException(exceptions);
            }
        }

    }
}
