using ModCore.Events.Collections;
using Serilog;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

#pragma warning disable CS8500

namespace ModCore.Events
{
    /// <summary>
    /// DCCM Event System
    /// </summary>
    public static class EventSystem
    {
        /// <summary>
        /// It is triggered when an event receiver is added.
        /// </summary>
        public static event Action<IEventReceiver>? OnAddReceiver;
        /// <summary>
        /// It is triggered when an event receiver is removed.
        /// </summary>
        public static event Action<IEventReceiver>? OnRemoveReceiver;

        public static event Action<Type, nint>? OnBeforeBroadcastEvent;
        public static event Action<Type, nint, nint>? OnAfterBroadcastEvent;
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


        /// <summary>
        /// Add an event receiver
        /// </summary>
        /// <param name="receiver"></param>
        public static void AddReceiver( IEventReceiver receiver )
        {
            eventReceivers.Add(receiver);
            OnAddReceiver?.Invoke(receiver);
        }
        /// <summary>
        /// Remove an event receiver
        /// </summary>
        /// <param name="receiver"></param>
        public static void RemoveReceiver( IEventReceiver receiver )
        {
            eventReceivers.Remove(receiver);
            OnRemoveReceiver?.Invoke(receiver);
        }

        /// <summary>
        /// Search for an event receiver of a specified type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T? FindReceiver<T>()
        {
            return eventReceivers.OfType<T>().FirstOrDefault();
        }
        /// <summary>
        /// Search for event receivers of a specified type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
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

        public static unsafe EventResult<TResult> BroadcastEvent<TEvent, TArg, TResult>( ref TArg arg, ExceptionHandingFlags flags = ExceptionHandingFlags.Default )
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

            fixed (void* parg = &arg)
            {
                OnBeforeBroadcastEvent?.Invoke(typeof(TEvent), (nint)parg);
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

                        fixed (void* parg = &arg)
                        {
                            OnAfterBroadcastEvent?.Invoke(typeof(TEvent), (nint)parg, (nint)Unsafe.AsPointer(ref result));
                        }
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
