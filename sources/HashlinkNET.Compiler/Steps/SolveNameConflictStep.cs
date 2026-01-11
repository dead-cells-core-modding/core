using HashlinkNET.Compiler.Data;

namespace HashlinkNET.Compiler.Steps
{
    internal class SolveNameConflictStep : CompileStep
    {
        public override void Execute( IDataContainer container )
        {
            var gdata = container.GetGlobalData<GlobalData>();
            HashSet<string> namespaces = [];

            REDO:

            var resolveAll = true;
            foreach (var v in gdata.Module.Types)
            {
                namespaces.Add(v.Namespace);
            }
            foreach(var v in gdata.Module.Types)
            {
                if (namespaces.Contains(v.FullName))
                {
                    v.Name = v.Name + "_";
                    resolveAll = false;
                }
            }

            if (!resolveAll)
            {
                goto REDO;
            }
        }
    }
}
