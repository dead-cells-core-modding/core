using ModCore.Events;

namespace TestRunner
{
    [Event]
    public interface ITestVoidEvent
    {
        void OnEvent();
    }

    [Event]
    public interface ITestSecondEvent
    {
        void OnEvent();
    }

    [Event]
    public interface ITestValueEvent
    {
        void OnEvent(int value);
    }

    [Event]
    public interface ITestRefEvent
    {
        void OnEvent(ref int value);
    }

    [Event]
    public interface ITestResultEvent
    {
        EventResult<int> OnEvent(int value);
    }

    [Event(true)]
    public interface ITestOnceEvent
    {
        void OnEvent();
    }

    [Event]
    public interface ITestEmptyEvent
    {
        void OnEvent();
    }

    [Event]
    public interface ITestEmptyResultEvent
    {
        EventResult<int> OnEvent(int value);
    }

    public class EventSystemTest
    {
        private static void Add(params IEventReceiver[] receivers)
        {
            foreach (var receiver in receivers)
            {
                EventSystem.AddReceiver(receiver);
            }
        }

        private static void Remove(params IEventReceiver[] receivers)
        {
            foreach (var receiver in receivers)
            {
                EventSystem.RemoveReceiver(receiver);
            }
        }

        private sealed class VoidReceiver : IEventReceiver, ITestVoidEvent
        {
            public int Priority { get; init; }
            public int Count;
            public Action? OnCalled;

            void ITestVoidEvent.OnEvent()
            {
                Count++;
                OnCalled?.Invoke();
            }
        }

        private sealed class MultiReceiver : IEventReceiver, ITestVoidEvent, ITestSecondEvent
        {
            public int First;
            public int Second;

            void ITestVoidEvent.OnEvent()
            {
                First++;
            }

            void ITestSecondEvent.OnEvent()
            {
                Second++;
            }
        }

        private sealed class ValueReceiver : IEventReceiver, ITestValueEvent
        {
            public int Last;

            void ITestValueEvent.OnEvent(int value)
            {
                Last = value;
            }
        }

        private sealed class RefReceiver : IEventReceiver, ITestRefEvent
        {
            void ITestRefEvent.OnEvent(ref int value)
            {
                value++;
            }
        }

        private sealed class ResultReceiver : IEventReceiver, ITestResultEvent
        {
            public int Priority { get; init; }
            public int Result { get; init; }
            public bool ReturnNull { get; init; }
            public bool Called;

            EventResult<int> ITestResultEvent.OnEvent(int value)
            {
                Called = true;
                return ReturnNull ? EventResult<int>.Null : new(Result);
            }
        }

        private sealed class OnceReceiver : IEventReceiver, ITestOnceEvent
        {
            public int Count;

            void ITestOnceEvent.OnEvent()
            {
                Count++;
            }
        }

        private sealed class ThrowingReceiver(Exception exception) : IEventReceiver, ITestVoidEvent
        {
            public int Priority { get; init; }

            void ITestVoidEvent.OnEvent()
            {
                throw exception;
            }
        }

        private sealed class BreakReceiver : IEventReceiver, ITestVoidEvent
        {
            void ITestVoidEvent.OnEvent()
            {
                throw new EventBreakException(new InvalidOperationException("break-inner"));
            }
        }

        [Fact]
        public void AddRemoveReceiver_FindReceiver()
        {
            var receiver = new VoidReceiver();
            Add(receiver);
            try
            {
                Assert.Same(receiver, EventSystem.FindReceiver<VoidReceiver>());
                Assert.Contains(receiver, EventSystem.FindReceivers<VoidReceiver>());
            }
            finally
            {
                Remove(receiver);
            }

            Assert.Null(EventSystem.FindReceiver<VoidReceiver>());
        }

        [Fact]
        public void AddReceiver_Duplicate_IsIgnored()
        {
            var receiver = new VoidReceiver();
            Add(receiver, receiver);
            try
            {
                EventSystem.BroadcastEvent<ITestVoidEvent>();
                Assert.Equal(1, receiver.Count);
            }
            finally
            {
                Remove(receiver);
            }
        }

        [Fact]
        public void Broadcast_NoReceivers_DoesNotThrow()
        {
            EventSystem.BroadcastEvent<ITestEmptyEvent>();
        }

        [Fact]
        public void Broadcast_PriorityOrder_IsAscending()
        {
            var order = new List<string>();
            var positive = new VoidReceiver { Priority = 10, OnCalled = () => order.Add("positive") };
            var negative = new VoidReceiver { Priority = -10, OnCalled = () => order.Add("negative") };
            var zero = new VoidReceiver { Priority = 0, OnCalled = () => order.Add("zero") };

            Add(positive, negative, zero);
            try
            {
                EventSystem.BroadcastEvent<ITestVoidEvent>();
                Assert.Equal(new[] { "negative", "zero", "positive" }, order);
            }
            finally
            {
                Remove(positive, negative, zero);
            }
        }

        [Fact]
        public void Broadcast_SamePriority_PreservesInsertionOrder()
        {
            var order = new List<string>();
            var a = new VoidReceiver { OnCalled = () => order.Add("a") };
            var b = new VoidReceiver { OnCalled = () => order.Add("b") };
            var c = new VoidReceiver { OnCalled = () => order.Add("c") };

            Add(a, b, c);
            try
            {
                EventSystem.BroadcastEvent<ITestVoidEvent>();
                Assert.Equal(new[] { "a", "b", "c" }, order);
            }
            finally
            {
                Remove(a, b, c);
            }
        }

        [Fact]
        public void Broadcast_ValueArgument_IsPassed()
        {
            var receiver = new ValueReceiver();
            Add(receiver);
            try
            {
                EventSystem.BroadcastEvent<ITestValueEvent, int>(42);
                Assert.Equal(42, receiver.Last);
            }
            finally
            {
                Remove(receiver);
            }
        }

        [Fact]
        public void Broadcast_RefArgument_IsMutated()
        {
            var receiver = new RefReceiver();
            Add(receiver);
            try
            {
                var value = 1;
                EventSystem.BroadcastEvent<ITestRefEvent, int>(ref value);
                Assert.Equal(2, value);
            }
            finally
            {
                Remove(receiver);
            }
        }

        [Fact]
        public void Broadcast_Result_FirstValueStopsChain()
        {
            var first = new ResultReceiver { Priority = 0, Result = 42 };
            var second = new ResultReceiver { Priority = 10, Result = 99 };

            Add(first, second);
            try
            {
                var result = EventSystem.BroadcastEvent<ITestResultEvent, int, int>(0);

                Assert.True(result.HasValue);
                Assert.Equal(42, result.Value);
                Assert.True(first.Called);
                Assert.False(second.Called);
            }
            finally
            {
                Remove(first, second);
            }
        }

        [Fact]
        public void Broadcast_Result_NullContinuesToNextReceiver()
        {
            var first = new ResultReceiver { Priority = 0, ReturnNull = true };
            var second = new ResultReceiver { Priority = 10, Result = 99 };

            Add(first, second);
            try
            {
                var result = EventSystem.BroadcastEvent<ITestResultEvent, int, int>(0);

                Assert.True(result.HasValue);
                Assert.Equal(99, result.Value);
                Assert.True(first.Called);
                Assert.True(second.Called);
            }
            finally
            {
                Remove(first, second);
            }
        }

        [Fact]
        public void Broadcast_Result_NoReceivers_ReturnsDefault()
        {
            var result = EventSystem.BroadcastEvent<ITestEmptyResultEvent, int, int>(0);
            Assert.False(result.HasValue);
        }

        [Fact]
        public void EventResult_DefaultAndNull_HaveNoValue()
        {
            Assert.False(default(EventResult<int>).HasValue);
            Assert.False(EventResult<int>.Null.HasValue);
            Assert.Equal(0, EventResult<int>.Null.Value);

            var value = new EventResult<int>(42);
            Assert.True(value.HasValue);
            Assert.Equal(42, value.Value);
        }

        [Fact]
        public void Broadcast_OnceEvent_SecondCallThrows()
        {
            var receiver = new OnceReceiver();
            Add(receiver);
            try
            {
                EventSystem.BroadcastEvent<ITestOnceEvent>();
                Assert.Equal(1, receiver.Count);

                Assert.Throws<InvalidOperationException>(
                    () => EventSystem.BroadcastEvent<ITestOnceEvent>());
            }
            finally
            {
                Remove(receiver);
            }
        }

        [Fact]
        public void Broadcast_DefaultException_RethrowsAndStops()
        {
            var throwing = new ThrowingReceiver(new InvalidOperationException("boom"));
            var normal = new VoidReceiver { Priority = 10 };

            Add(throwing, normal);
            try
            {
                Assert.Throws<InvalidOperationException>(
                    () => EventSystem.BroadcastEvent<ITestVoidEvent>());
                Assert.Equal(0, normal.Count);
            }
            finally
            {
                Remove(throwing, normal);
            }
        }

        [Fact]
        public void Broadcast_Continue_AggregatesExceptions()
        {
            var first = new ThrowingReceiver(new InvalidOperationException("first"));
            var second = new ThrowingReceiver(new ArgumentException("second")) { Priority = 10 };

            Add(first, second);
            try
            {
                var ex = Assert.Throws<AggregateException>(
                    () => EventSystem.BroadcastEvent<ITestVoidEvent>(
                        EventSystem.ExceptionHandingFlags.Continue));

                Assert.Equal(2, ex.InnerExceptions.Count);
            }
            finally
            {
                Remove(first, second);
            }
        }

        [Fact]
        public void Broadcast_NoThrow_ContinuesWithoutThrowing()
        {
            var throwing = new ThrowingReceiver(new InvalidOperationException("boom"));
            var normal = new VoidReceiver { Priority = 10 };

            Add(throwing, normal);
            try
            {
                EventSystem.BroadcastEvent<ITestVoidEvent>(
                    EventSystem.ExceptionHandingFlags.NoThrow);

                Assert.Equal(1, normal.Count);
            }
            finally
            {
                Remove(throwing, normal);
            }
        }

        [Fact]
        public void Broadcast_EventBreak_RethrowsInnerException()
        {
            var receiver = new BreakReceiver();
            Add(receiver);
            try
            {
                var ex = Assert.Throws<InvalidOperationException>(
                    () => EventSystem.BroadcastEvent<ITestVoidEvent>());
                Assert.Equal("break-inner", ex.Message);
            }
            finally
            {
                Remove(receiver);
            }
        }

        [Fact]
        public void Broadcast_AddReceiverDuringEvent_NotCalledInCurrentRound()
        {
            var added = new VoidReceiver();
            var adder = new VoidReceiver
            {
                OnCalled = () => EventSystem.AddReceiver(added),
            };

            Add(adder);
            try
            {
                EventSystem.BroadcastEvent<ITestVoidEvent>();
                Assert.Equal(1, adder.Count);
                Assert.Equal(0, added.Count);

                EventSystem.BroadcastEvent<ITestVoidEvent>();
                Assert.Equal(2, adder.Count);
                Assert.Equal(1, added.Count);
            }
            finally
            {
                Remove(adder, added);
            }
        }

        [Fact]
        public void Broadcast_RemoveSelfDuringEvent_StopsFutureCalls()
        {
            VoidReceiver? receiver = null;
            receiver = new VoidReceiver
            {
                OnCalled = () => EventSystem.RemoveReceiver(receiver!),
            };

            Add(receiver);
            try
            {
                EventSystem.BroadcastEvent<ITestVoidEvent>();
                Assert.Equal(1, receiver.Count);

                EventSystem.BroadcastEvent<ITestVoidEvent>();
                Assert.Equal(1, receiver.Count);
            }
            finally
            {
                Remove(receiver);
            }
        }

        [Fact]
        public void MultipleEventInterfaces_AllReceive()
        {
            var receiver = new MultiReceiver();
            Add(receiver);
            try
            {
                EventSystem.BroadcastEvent<ITestVoidEvent>();
                EventSystem.BroadcastEvent<ITestSecondEvent>();

                Assert.Equal(1, receiver.First);
                Assert.Equal(1, receiver.Second);
            }
            finally
            {
                Remove(receiver);
            }
        }

        [Fact]
        public void OnAddRemoveReceiver_EventsFire()
        {
            var added = new List<IEventReceiver>();
            var removed = new List<IEventReceiver>();

            void OnAdd(IEventReceiver receiver) => added.Add(receiver);
            void OnRemove(IEventReceiver receiver) => removed.Add(receiver);

            EventSystem.OnAddReceiver += OnAdd;
            EventSystem.OnRemoveReceiver += OnRemove;

            var receiver = new VoidReceiver();
            try
            {
                EventSystem.AddReceiver(receiver);
                Assert.Contains(receiver, added);

                EventSystem.RemoveReceiver(receiver);
                Assert.Contains(receiver, removed);
            }
            finally
            {
                EventSystem.OnAddReceiver -= OnAdd;
                EventSystem.OnRemoveReceiver -= OnRemove;
                Remove(receiver);
            }
        }

    }
}

