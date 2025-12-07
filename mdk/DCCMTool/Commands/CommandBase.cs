using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCCMTool.Commands
{
    internal interface ICommandBase
    {
        abstract static Type GetArgType();
        void SetArguments(object args);
        Task ExecuteAsync();
    }
    internal abstract class CommandBase<TArg> : ICommandBase
    {
        protected TArg Arguments { get; private set; } = default!;
        public static Type GetArgType()
        {
            return typeof(TArg);
        }

        public virtual void Execute()
        {
            throw new NotImplementedException();
        }

        public virtual Task ExecuteAsync()
        {
            Execute();
            return Task.CompletedTask;
        }

        public void SetArguments(object args)
        {
            Arguments = (TArg)args;
        }
    }
}
