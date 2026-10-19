using BenchmarkDotNet.Attributes;
using ModCore.Events;

namespace DCCMBenchmark
{
    [Event]
    public interface IBenchVoidEvent
    {
        void OnEvent();
    }

    [Event]
    public interface IBenchValueEvent
    {
        void OnEvent(int value);
    }

    [Event]
    public interface IBenchResultEvent
    {
        EventResult<int> OnEvent(int value);
    }

    internal sealed class BenchVoidReceiver : IEventReceiver, IBenchVoidEvent
    {
        public int Count;

        void IBenchVoidEvent.OnEvent()
        {
            Count++;
        }
    }

    internal sealed class BenchValueReceiver : IEventReceiver, IBenchValueEvent
    {
        public int Last;

        void IBenchValueEvent.OnEvent(int value)
        {
            Last = value;
        }
    }

    internal sealed class BenchNullResultReceiver : IEventReceiver, IBenchResultEvent
    {
        EventResult<int> IBenchResultEvent.OnEvent(int value)
        {
            return EventResult<int>.Null;
        }
    }

    internal sealed class BenchThrowingReceiver : IEventReceiver, IBenchVoidEvent
    {
        private readonly InvalidOperationException exception = new("benchmark");

        void IBenchVoidEvent.OnEvent()
        {
            throw exception;
        }
    }

    /// <summary>
    /// EventSystem broadcast scaling: no receiver, void/value/result events,
    /// and FindReceivers enumeration.
    /// </summary>
    [MemoryDiagnoser]
    public class EventSystemBenchmarks
    {
        [Params(0, 1, 4, 16, 64)]
        public int ReceiverCount;

        private readonly List<BenchVoidReceiver> voidReceivers = [];
        private readonly List<BenchValueReceiver> valueReceivers = [];
        private readonly List<BenchNullResultReceiver> resultReceivers = [];

        [GlobalSetup]
        public void Setup()
        {
            GameBootstrap.EnsureInitialized();

            for (var i = 0; i < ReceiverCount; i++)
            {
                var voidReceiver = new BenchVoidReceiver();
                var valueReceiver = new BenchValueReceiver();
                var resultReceiver = new BenchNullResultReceiver();

                EventSystem.AddReceiver(voidReceiver);
                EventSystem.AddReceiver(valueReceiver);
                EventSystem.AddReceiver(resultReceiver);

                voidReceivers.Add(voidReceiver);
                valueReceivers.Add(valueReceiver);
                resultReceivers.Add(resultReceiver);
            }
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            foreach (var receiver in voidReceivers)
            {
                EventSystem.RemoveReceiver(receiver);
            }
            foreach (var receiver in valueReceivers)
            {
                EventSystem.RemoveReceiver(receiver);
            }
            foreach (var receiver in resultReceivers)
            {
                EventSystem.RemoveReceiver(receiver);
            }

            voidReceivers.Clear();
            valueReceivers.Clear();
            resultReceivers.Clear();
        }

        [Benchmark]
        public void Broadcast_Void()
        {
            EventSystem.BroadcastEvent<IBenchVoidEvent>();
        }

        [Benchmark]
        public void Broadcast_Value()
        {
            EventSystem.BroadcastEvent<IBenchValueEvent, int>(42);
        }

        [Benchmark]
        public void Broadcast_Result_AllNull()
        {
            _ = EventSystem.BroadcastEvent<IBenchResultEvent, int, int>(42).HasValue;
        }

        [Benchmark]
        public int FindReceivers()
        {
            return EventSystem.FindReceivers<BenchVoidReceiver>().Count();
        }
    }

    /// <summary>
    /// EventSystem add/remove registration cost.
    /// </summary>
    [MemoryDiagnoser]
    public class EventSystemRegistrationBenchmarks
    {
        [Params(1, 4, 16)]
        public int ReceiverCount;

        private readonly List<BenchVoidReceiver> receivers = [];

        [GlobalSetup]
        public void Setup()
        {
            GameBootstrap.EnsureInitialized();

            // Initialize the per-event receiver cache so add/remove measures
            // the realistic cache-update path.
            EventSystem.BroadcastEvent<IBenchVoidEvent>();

            for (var i = 0; i < ReceiverCount; i++)
            {
                receivers.Add(new BenchVoidReceiver());
            }
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            foreach (var receiver in receivers)
            {
                EventSystem.RemoveReceiver(receiver);
            }
            receivers.Clear();
        }

        [Benchmark]
        public void AddRemove()
        {
            foreach (var receiver in receivers)
            {
                EventSystem.AddReceiver(receiver);
            }
            foreach (var receiver in receivers)
            {
                EventSystem.RemoveReceiver(receiver);
            }
        }
    }

    /// <summary>
    /// EventSystem exception path with logging disabled.
    /// </summary>
    [MemoryDiagnoser]
    public class EventSystemExceptionBenchmarks
    {
        private BenchThrowingReceiver throwing = null!;
        private BenchVoidReceiver normal = null!;

        [GlobalSetup]
        public void Setup()
        {
            GameBootstrap.EnsureInitialized();

            throwing = new BenchThrowingReceiver();
            normal = new BenchVoidReceiver();

            EventSystem.AddReceiver(throwing);
            EventSystem.AddReceiver(normal);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            EventSystem.RemoveReceiver(throwing);
            EventSystem.RemoveReceiver(normal);
        }

        [Benchmark]
        public void Broadcast_NoThrow_Quiet()
        {
            EventSystem.BroadcastEvent<IBenchVoidEvent>(
                EventSystem.ExceptionHandingFlags.NoThrow |
                EventSystem.ExceptionHandingFlags.Quiet);
        }
    }
}
