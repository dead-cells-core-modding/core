#pragma warning disable IDE0130
namespace System.Runtime.CompilerServices
#pragma warning restore IDE0130
{
    [AttributeUsage(AttributeTargets.Assembly)]
#pragma warning disable CS9113
    internal class IgnoresAccessChecksToAttribute(string asmName) : Attribute
#pragma warning restore CS9113
    {
    }
}
