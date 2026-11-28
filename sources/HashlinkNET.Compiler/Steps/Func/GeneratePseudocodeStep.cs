using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Pseudocode;
using MonoMod.Utils;

namespace HashlinkNET.Compiler.Steps.Func
{
    internal class GeneratePseudocodeStep : ParallelCompileStep<HlFunction>
    {
        private const int GcInterval = 7000;
        private int processedCount = 0;
        protected override void Execute( IDataContainer container, HlFunction item, int index )
        {
            var fd = container.GetData<FuncData>(item);

            var compiler = new FunctionCompiler(item,
                    fd,
                    container);
            compiler.Compile();

            // Trigger a background, non-blocking collection periodically instead
            // of a stop-the-world GC.Collect from a parallel worker.
            if (Interlocked.Increment(ref processedCount) % GcInterval == 0)
            {
                GC.Collect(2, GCCollectionMode.Optimized,
                    blocking: false, compacting: false);
            }
        }
        protected override IReadOnlyList<HlFunction> GetItems( IDataContainer container )
        {
            var functions = container.GetGlobalData<GlobalData>().Code.Functions;
            // Schedule the largest functions first (longest-processing-time
            // first) so the few huge functions do not form a serial tail.
            var sorted = functions.ToArray();
            Array.Sort(sorted, static (a, b) => b.Opcodes.Length.CompareTo(a.Opcodes.Length));
            return sorted;
        }
    }
}
