using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HaxeProxy.Codegen
{
    internal abstract class BuiltinModifier
    {
        public string? TargetName
        {
            get; set;
        }
        public abstract void Modify( TypeDefinition type );
        public virtual bool CanModify( TypeDefinition type )
        {
            return type.FullName.Equals( TargetName );
        }
        public virtual MethodBase FindMethod( string name )
        {
            var result = GetType().GetMethod(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic |
                BindingFlags.Static);
            Debug.Assert( result != null );
            return result;
        }

    }
}
