using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler.Steps
{
    abstract class ParallelCompileStep<T> : CompileStep
    {
        private readonly ConcurrentQueue<Action> syncRun = [];
        private IReadOnlyList<T> items = null!;
        private int curItemId;
        protected virtual bool SupportParalle => true;
        public override sealed void Execute( IDataContainer container )
        {
            var config = container.GetGlobalData<CompileConfig>();
            Initialize(container);
            items = GetItems(container);
            if (!SupportParalle || !config.AllowParalle)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    Execute(container, items[i], i);
                }
            }
            else
            {
                var tasks = new Task[Environment.ProcessorCount];
                for (int i = 0; i < tasks.Length; i++)
                {
                    tasks[i] = Task.Factory.StartNew(StepTaskRunner, container);
                }
                Task.WaitAll(tasks);
            }
            while (syncRun.TryDequeue(out var action))
            {
                action();
            }
            PostProcessing(container);
        }
        private void StepTaskRunner(object? obj)
        {
            var container = (IDataContainer)obj!;
            while (true)
            {
                var id = Interlocked.Increment(ref curItemId) - 1;
                if (id >= items.Count)
                {
                    return;
                }
                Execute(container, items[id], id);
            }
        }

        protected void RunSync( Action action )
        {
            syncRun.Enqueue( action );
        }

        protected abstract void Execute( IDataContainer container, T item, int index );

        protected abstract IReadOnlyList<T> GetItems( IDataContainer container );
        protected virtual void Initialize( IDataContainer container )
        {
        
        }
        protected virtual void PostProcessing( IDataContainer container )
        {
        
        }
    }
}
