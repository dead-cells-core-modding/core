using HashlinkNET.Compiler.Steps;
using System.Collections.Concurrent;

namespace HashlinkNET.Compiler
{
    public abstract class BaseCompiler
    {
        internal readonly IDataContainer data = new DataContainer();

        private readonly Queue<CompileStep> steps = new();
        private bool compiled = false;

        /// <summary>
        /// Comma/semicolon separated step names (short or full) that should be
        /// skipped. Used to bisect which optimization step breaks generated IL.
        /// Example: <c>DCCM_DISABLE_STEPS=StackStoreEliminationStep,PeepholeIRStep</c>
        /// </summary>
        private static readonly HashSet<string> disabledSteps = ParseDisabledSteps();
        private static readonly ConcurrentDictionary<Type, byte> loggedDisabled = new();

        private static HashSet<string> ParseDisabledSteps()
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var raw = Environment.GetEnvironmentVariable("DCCM_DISABLE_STEPS");
            if (string.IsNullOrWhiteSpace(raw))
            {
                return result;
            }
            foreach (var part in raw.Split([',', ';'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                result.Add(part);
            }
            return result;
        }

        private static bool IsDisabled( Type stepType )
        {
            return disabledSteps.Count != 0 &&
                (disabledSteps.Contains(stepType.Name) ||
                 disabledSteps.Contains(stepType.FullName ?? string.Empty));
        }

        internal event Action<IDataContainer, CompileStep>? OnBeforeRunStep;
        internal event Action<IDataContainer, CompileStep>? OnAfterRunStep;

        protected virtual void CompileImpl()
        {
            InstallSteps();
            BeforeRun();
            RunSteps();
            AfterRun();
        }
        public virtual void Compile()
        {
            if (compiled)
            {
                throw new InvalidOperationException();
            }

            CompileImpl();

            data.Clear();
            compiled = true;
        }

        internal T AddStep<T>() where T : CompileStep, new()
        {
            return AddStep(new T());
        }

        internal T AddStep<T>( T step ) where T : CompileStep
        {
            if (IsDisabled(step.GetType()))
            {
                if (loggedDisabled.TryAdd(step.GetType(), 0))
                {
                    Console.Error.WriteLine($"[step] disabled: {step.GetType().Name}");
                }
                return step;
            }
            steps.Enqueue(step);
            return step;
        }

        protected abstract void InstallSteps();

        protected virtual void BeforeRun()
        {
        }

        protected virtual void AfterRun()
        {
        }

        protected void RunSteps()
        {
            while (steps.TryDequeue(out var step))
            {
                OnBeforeRunStep?.Invoke(data, step);
                step.Execute(data);
                OnAfterRunStep?.Invoke(data, step);
            }
        }
    }
}
