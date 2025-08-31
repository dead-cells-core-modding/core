
using NonPublicNativeMembers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using System.Reflection.Metadata;
var libs = args[..^1];

var manager = NativeMembersManager.Create();

manager.Generate(libs);

var savePath = Path.GetFullPath(args[^1]);
File.WriteAllBytes(savePath, manager.Save());
