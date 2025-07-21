using ModCore.Events.Collections;
using Serilog;
using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace ModCore.Events
{
    public static class EventSystem
    {
        public static event Action<IEventReceiver>? OnAddReceiver;
        public static event Action<IEventReceiver>? OnRemoveReceiver;
        private static ILogger Logger { get; } = Log.Logger.ForContext("SourceContext", "EventSystem");
        [Flags]
        public enum ExceptionHandingFlags
        {
            Default = 0,

            Continue = 1,
            NoThrow = 2,
            Quiet = 4,
            DirectRethrow = 8
        }
        private static readonly EventReceiverList eventReceivers = [];

        

        public static void AddReceiver( IEventReceiver receiver )
        {
            eventReceivers.Add(receiver);
            OnAddReceiver?.Invoke(receiver);
        }
        public static void RemoveReceiver( IEventReceiver receiver )
        {
            eventReceivers.Remove(receiver);
            OnRemoveReceiver?.Invoke(receiver);
        }

        public static T? FindReceiver<T>()
        {
            return eventReceivers.OfType<T>().FirstOrDefault();
        }
        public static IEnumerable<T> FindReceivers<T>()
        {
            return eventReceivers.OfType<T>();
        }
        [StackTraceHidden]
        public static void BroadcastEvent<TEvent>( ExceptionHandingFlags flags = ExceptionHandingFlags.Default )
        {
            BroadcastEvent<TEvent, int>(0, flags);
        }
        [StackTraceHidden]
        public static void BroadcastEvent<TEvent, TArg>( TArg arg, ExceptionHandingFlags flags = ExceptionHandingFlags.Default )
                        where TArg : allows ref struct
        {
            BroadcastEvent<TEvent, TArg>(ref arg, flags);
        }
        [StackTraceHidden]
        public static void BroadcastEvent<TEvent, TArg>(ref TArg arg, ExceptionHandingFlags flags = ExceptionHandingFlags.Default )
                        where TArg : allows ref struct
        {
            _ = BroadcastEvent<TEvent, TArg, object?>(ref arg, flags);
        }
        [StackTraceHidden]
        public static EventResult<TResult> BroadcastEvent<TEvent, TArg, TResult>( TArg arg, ExceptionHandingFlags flags = ExceptionHandingFlags.Default )
                       where TArg : allows ref struct
        {
            return BroadcastEvent<TEvent, TArg, TResult>(ref arg, flags);
        }
        public static EventResult<TResult> BroadcastEvent<TEvent, TArg, TResult>( ref TArg arg, ExceptionHandingFlags flags = ExceptionHandingFlags.Default )
                        where TArg : allows ref struct
        {
            if (EventCaller<TEvent>.IsCallOnce)
            {
                if (EventCaller<TEvent>.IsCalled)
                {
                    throw new InvalidOperationException("An event that should only be called once was called multiple times");
                }
                Logger.Debug("Broadcast Global Event: {Name}", typeof(TEvent).Name);
            }
            List<Exception>? exceptions = null;
            var receivers = EventCaller<TEvent>.IsCallOnce ? eventReceivers : (IEnumerable<IEventReceiver>)EventReceiversCache<TEvent>.receivers;
            foreach (var module in receivers)
            {
                if (module is TEvent ev)
                {
                    try
                    {
                        EventCaller<TEvent>.Invoke(ev, ref arg, out EventResult<TResult> result);
                        if (result.HasValue)
                        {
                            return result;
                        }
                    }
                    catch (EventBreakException ex)
                    {
                        if (ex.InnerException != null)
                        {
                            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                        }
                        throw;
                    }
                    catch (Exception ex)
                    {
                        if (flags == ExceptionHandingFlags.DirectRethrow)
                        {
                            throw;
                        }
                        if (!flags.HasFlag(ExceptionHandingFlags.Quiet))
                        {
                            Logger.Error(ex, "An exception occurred when executing event {evName}", typeof(TEvent).Name);
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
            return default;
        }

    }
}
