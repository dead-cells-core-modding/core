using dc.hxd;
using dc.hxd.res;

namespace ModCore.Utilities
{
    /// <summary>
    /// Provides helpers for loading game resources.
    /// </summary>
    public static class ResLoadUtils
    {
        extension( Res )
        {
            /// <summary>
            /// Loads a resource by path, optionally prefixed with a namespace.
            /// </summary>
            /// <param name="path">The resource path.</param>
            /// <param name="ns">The optional namespace prefix.</param>
            /// <returns>The loaded resource, or <see langword="null"/> if the path is empty.</returns>
            public static Any? LoadResEx( string path, string? ns = null )
            {
                if (!string.IsNullOrEmpty(path))
                {
                    return Res.Class.load((ns + "/" + path).AsHaxeString());
                }
                return Res.Class.load(path.AsHaxeString());
            }
            /// <summary>
            /// Attempts to load a resource by path, optionally prefixed with a namespace.
            /// </summary>
            /// <param name="path">The resource path.</param>
            /// <param name="res">The loaded resource, or <see langword="null"/> if loading fails.</param>
            /// <param name="ns">The optional namespace prefix.</param>
            /// <returns><see langword="true"/> if the resource was loaded successfully; otherwise, <see langword="false"/>.</returns>
            public static bool TryLoadResEx( string path, out Any? res, string? ns = null )
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
