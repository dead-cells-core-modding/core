using ModCore.Events.Collections;
using MonoMod.Utils;
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

        private static ILogger Logger { get; } = Log.Logger.ForContext("SourceContext", "EventSystem");
        /// <summary>
        /// Specifies how exceptions thrown by event handlers are handled.
        /// </summary>
        [Flags]
        public enum ExceptionHandingFlags
        {
            /// <summary>
            /// Rethrows the first exception.
            /// </summary>
            Default = 0,

            /// <summary>
            /// Continues dispatching to the remaining receivers.
            /// </summary>
            Continue = 1,
            /// <summary>
            /// Does not rethrow exceptions.
            /// </summary>
            NoThrow = 2,
            /// <summary>
            /// Does not log exceptions.
            /// </summary>
            Quiet = 4,
            /// <summary>
            /// Rethrows exceptions directly without wrapping them.
            /// </summary>
            DirectRethrow = 8
        }
        private static readonly EventReceiverList eventReceivers = [];


        /// <summary>
        /// Add an event receiver
        /// </summary>
        /// <param name="receiver">The receiver to add.</param>
        public static void AddReceiver( IEventReceiver receiver )
        {
            eventReceivers.Add(receiver);
            OnAddReceiver?.Invoke(receiver);
        }
        /// <summary>
        /// Remove an event receiver
        /// </summary>
        /// <param name="receiver">The receiver to remove.</param>
        public static void RemoveReceiver( IEventReceiver receiver )
        {
            eventReceivers.Remove(receiver);
            OnRemoveReceiver?.Invoke(receiver);
        }

        /// <summary>
        /// Search for an event receiver of a specified type
        /// </summary>
        /// <typeparam name="T">The receiver type to search for.</typeparam>
        /// <returns>The first matching receiver, or <see langword="null"/> if none exists.</returns>
        public static T? FindReceiver<T>()
        {
            return eventReceivers.OfType<T>().FirstOrDefault();
        }
        /// <summary>
        /// Search for event receivers of a specified type
        /// </summary>
        /// <typeparam name="T">The receiver type to search for.</typeparam>
        /// <returns>All matching receivers.</returns>
        public static IEnumerable<T> FindReceivers<T>()
        {
            return eventReceivers.OfType<T>();
        }
        /// <summary>
        /// Broadcasts an event without arguments.
        /// </summary>
        /// <typeparam name="TEvent">The event interface type.</typeparam>
        /// <param name="flags">The exception handling behavior.</param>
        [StackTraceHidden]
        public static void BroadcastEvent<TEvent>( ExceptionHandingFlags flags = ExceptionHandingFlags.Default )
        {
            BroadcastEvent<TEvent, int>(0, flags);
        }
        /// <summary>
        /// Broadcasts an event with an argument.
        /// </summary>
        /// <typeparam name="TEvent">The event interface type.</typeparam>
        /// <typeparam name="TArg">The argument type.</typeparam>
        /// <param name="arg">The event argument.</param>
        /// <param name="flags">The exception handling behavior.</param>
        [StackTraceHidden]
        public static void BroadcastEvent<TEvent, TArg>( TArg arg, ExceptionHandingFlags flags = ExceptionHandingFlags.Default )
                        where TArg : allows ref struct
        {
            BroadcastEvent<TEvent, TArg>(ref arg, flags);
        }
        /// <summary>
        /// Broadcasts an event with an argument passed by reference.
        /// </summary>
        /// <typeparam name="TEvent">The event interface type.</typeparam>
        /// <typeparam name="TArg">The argument type.</typeparam>
        /// <param name="arg">The event argument.</param>
        /// <param name="flags">The exception handling behavior.</param>
        [StackTraceHidden]
        public static void BroadcastEvent<TEvent, TArg>( ref TArg arg, ExceptionHandingFlags flags = ExceptionHandingFlags.Default )
                        where TArg : allows ref struct
        {
            _ = BroadcastEvent<TEvent, TArg, object?>(ref arg, flags);
        }
        /// <summary>
        /// Broadcasts an event with an argument and returns the first handler result.
        /// </summary>
        /// <typeparam name="TEvent">The event interface type.</typeparam>
        /// <typeparam name="TArg">The argument type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="arg">The event argument.</param>
        /// <param name="flags">The exception handling behavior.</param>
        /// <returns>The first result produced by a handler, or an empty result.</returns>
        [StackTraceHidden]
        public static EventResult<TResult> BroadcastEvent<TEvent, TArg, TResult>( TArg arg, ExceptionHandingFlags flags = ExceptionHandingFlags.Default )
                       where TArg : allows ref struct
        {
            return BroadcastEvent<TEvent, TArg, TResult>(ref arg, flags);
        }

        /// <summary>
        /// Broadcasts an event with an argument passed by reference and returns the first handler result.
        /// </summary>
        /// <typeparam name="TEvent">The event interface type.</typeparam>
        /// <typeparam name="TArg">The argument type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="arg">The event argument.</param>
        /// <param name="flags">The exception handling behavior.</param>
        /// <returns>The first result produced by a handler, or an empty result.</returns>
        public static EventResult<TResult> BroadcastEvent<TEvent, TArg, TResult>( ref TArg arg, ExceptionHandingFlags flags = ExceptionHandingFlags.Default )
                        where TArg : allows ref struct
        {
            if (EventCaller<TEvent>.Kind.Has(EventAttribute.EventKind.ShowInLog))
            {
                Logger.Debug("Broadcast Global Event: {Name}", typeof(TEvent).Name);
            }
            if (EventCaller<TEvent>.IsCallOnce)
            {
                if (EventCaller<TEvent>.IsCalled)
                {
                    throw new InvalidOperationException("An event that should only be called once was called multiple times");
                }
            }

            List<Exception>? exceptions = null;
            var receivers = EventCaller<TEvent>.IsCallOnce ? eventReceivers : EventReceiversCache<TEvent>.GetReceivers();
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
