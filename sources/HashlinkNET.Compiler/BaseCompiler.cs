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

        public abstract void Compile();

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

        protected void RunSteps()
        {
            while (steps.TryDequeue(out var step))
            {
                step.Execute(data);
            }
        }
    }
}
