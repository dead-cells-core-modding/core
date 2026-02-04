using dc.hxd;
using dc.hxd.res;

namespace ModCore.Utilities
{
    public static class ResLoadUtils
    {
        extension( Res )
        {
            public static Any? LoadResEx( string path, string? ns = null )
            {
                if (!string.IsNullOrEmpty(path))
                {
                    return Res.Class.load((ns + "/" + path).AsHaxeString());
                }
                return Res.Class.load(path.AsHaxeString());
            }

            public static bool TryLoadResEx( string path, out Any? res, string? ns = null)
            {
                res = null;
                try
                {
                    res = LoadResEx(path, ns);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }
    }
}
