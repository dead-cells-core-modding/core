using System;

namespace HaxeProxy.Runtime.Internals
{
    /// <summary>
    /// Marks a generated pseudocode method whose translated IL cannot exactly
    /// match the native Hashlink semantics.  Consumers should fall back to the
    /// native implementation for such methods.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class HashlinkShouldUseNativeAttribute : Attribute
    {
    }
}
