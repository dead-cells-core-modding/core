using System;
using System.Collections.Generic;
using System.Text;

namespace HaxeProxy.Runtime.Internals
{
    /// <summary>
    /// Marks a method or class as an alternate implementation that should be ignored by proxy generation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class HashlinkAltAttribute : Attribute
    {
    }
}
