using HashlinkNET.Compiler.Steps;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashlinkNET.Compiler
{
    public abstract class BaseCompiler
    {
        internal readonly IDataContainer data = new DataContainer();

        private readonly Queue<CompileStep> steps = new();
        private bool compiled = false;

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
