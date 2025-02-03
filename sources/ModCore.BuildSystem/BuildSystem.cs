
using ModCore.BuildSystem;

if (args.Length == 0)
{
    return -1;
}

var feat = args[0];
if(feat == "--build-modinfo")
{
    return new BuildModInfo(Utils.CleanPath(args[1]), Utils.CleanPath(args[2]), Utils.CleanPath(args[3])).Execute();
}

return -1;
