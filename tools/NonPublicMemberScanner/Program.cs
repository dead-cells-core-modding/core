
using NonPublicNativeMembers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using System.Reflection.Metadata;

var libPath = args[0];

List<string> libs = [];

foreach (var v in args[1..])
{
    var p = Path.Combine(libPath, v);
    var ext = Path.GetExtension(v);
    if(ext != ".so" && ext != ".dll")
    {
        continue;
    }
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        if (!File.Exists(Path.ChangeExtension(p, "pdb")))
        {
            continue;
        }
    }
    libs.Add(p);
}

var manager = NativeMembersManager.Create();

manager.Generate([..libs]);

File.WriteAllBytes(Path.Combine(libPath, "nativemembers.json"), manager.Save());
