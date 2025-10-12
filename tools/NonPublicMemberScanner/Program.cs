
using NonPublicNativeMembers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using System.Reflection.Metadata;

var libPath = args[0];

List<string> libs = [];

foreach (var v in Directory.EnumerateFiles(libPath))
{
    var name = Path.GetFileNameWithoutExtension(v);
    if(name == "modcorenative")
    {
        continue;
    }
    var ext = Path.GetExtension(v);
    if(ext != ".so" && ext != ".dll")
    {
        continue;
    }
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        if (!File.Exists(Path.ChangeExtension(v, "pdb")))
        {
            continue;
        }
    }
    libs.Add(v);
}

var manager = NativeMembersManager.Create();

manager.Generate([..libs]);

File.WriteAllBytes(Path.Combine(libPath, "nativemembers.json"), manager.Save());
