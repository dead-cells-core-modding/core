using CommandLine;
using CommandLine.Text;
using DCCMTool.Commands;
using System.Diagnostics;
using System.Reflection;

namespace DCCMTool
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Dictionary<Type, Type> commands = [];
            foreach(var v in typeof(Program).Assembly.GetTypes()
                .Where(x => !x.IsAbstract &&
                            x.IsClass &&
                            x.IsAssignableTo(typeof(ICommandBase)))
                )
            {
                var md_GetArgType = v.GetMethod("GetArgType", BindingFlags.Static | 
                    BindingFlags.Public | BindingFlags.FlattenHierarchy);

                Debug.Assert(md_GetArgType != null);

                var targ = (Type?)md_GetArgType.Invoke(null, null);

                Debug.Assert(targ != null);

                commands.Add(targ, v);
            }

            var result = new Parser(settings =>
            {
                settings.AllowMultiInstance = true;
            }).ParseArguments(args, [.. commands.Keys]);

            if (result.Value == null)
            {
                HelpText ht = HelpText.AutoBuild(result, 300);
                Console.Error.WriteLine(ht.ToString());
                Environment.Exit(-1);
                return;
            }

            var t = result.Value.GetType();
            var commandType = commands[t];

            var command = (ICommandBase) Activator.CreateInstance(commandType)!;
            command.SetArguments(result.Value);
            await command.ExecuteAsync();
        }
    }
}
