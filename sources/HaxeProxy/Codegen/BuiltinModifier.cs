using Mono.Cecil;
using System.Diagnostics;
using System.Reflection;

namespace HaxeProxy.Codegen
{
    internal abstract class BuiltinModifier
    {
        public string? TargetName {
            get; set;
        }
        public abstract void Modify( TypeDefinition type );
        public virtual bool CanModify( TypeDefinition type )
        {
            return string.Equals(type.FullName, TargetName, StringComparison.Ordinal);
        }
        public virtual MethodBase FindMethod( string name )
        {
            var result = GetType().GetMethod(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic |
                BindingFlags.Static);
            Debug.Assert(result != null);
            return result;
        }

    }
}
