using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCCMTool.Commands
{
    internal abstract class CommandBase<TArg> : Command<TArg> where TArg : CommandSettings
    {
        protected TArg Arguments { get; private set; } = default!;
        public static Type GetArgType()
        {
            return typeof(TArg);
        }

        public virtual int Execute()
        {
            throw new NotImplementedException();
        }
        public override int Execute(CommandContext context, TArg settings, CancellationToken cancellationToken)
        {
            Arguments = settings;
            var task = ExecuteAsync();
            task.Wait(cancellationToken);
            return task.Result;
        }

        public virtual Task<int> ExecuteAsync()
        {
            return Task.FromResult(Execute());
        }
    }
}
