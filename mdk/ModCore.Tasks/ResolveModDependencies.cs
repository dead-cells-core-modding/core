using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ModCore.Tasks
{
    public class ResolveModDependencies : Task
    {
        [Required]
        public string ModsRoot
        {
            get; set;
        }
        [Required]
        public ITaskItem[] ModNames
        {
            get; set;
        }
        [Output]
        public ITaskItem[] Output
        {
            get; set;
        }
        public override bool Execute()
        {
            bool hasMissing = false;
            List<ITaskItem> result = new();
            foreach (var v in ModNames)
            {
                var fullname = v.GetMetadata("Identity").Split('-');
                Version version = null;
                if (fullname.Length > 2 || 
                    
                    fullname.Length == 2 && !Version.TryParse(fullname[1], out version)
                    
                    )
                {
                    Log.LogError("{0} is an invalid mod name", v);
                    hasMissing = true;
                    continue;
                }

                string name = fullname[0];
                var root = Path.GetFullPath(Path.Combine(ModsRoot, name));
                var mip = Path.Combine(root, "modinfo.json");
                if (!File.Exists(mip))
                {
                    Log.LogError("The specific mod '{0}' was not found", name);
                    hasMissing = true;
                    continue;
                }
                var modinfo = JObject.Parse(File.ReadAllText(mip));
                if (modinfo["name"]?.ToString() != name)
                {
                    Log.LogError(null, null, null, mip, 0, 0, 0, 0, "ModInfo does not match directory structure");
                    hasMissing = true;
                    continue;
                }
                Version modVer = Version.Parse(modinfo["version"].ToString());
                var item = new TaskItem();
                result.Add(item);

                if (version != null)
                {
                    item.SetMetadata("RequestVersion", version.ToString());
                    if (modVer < version)
                    {
                        Log.LogError("The requested mod version {0} is greater than the existing mod version {1}", version, modVer);
                    }
                }
                item.ItemSpec = fullname[0];
                item.SetMetadata("Name", fullname[0]);
                item.SetMetadata("Version", modVer.ToString());
                item.SetMetadata("ModInfoPath", mip);
                item.SetMetadata("ModRoot", root);
                
            }
            Output = result.ToArray();
            return !hasMissing;
        }
    }
}
