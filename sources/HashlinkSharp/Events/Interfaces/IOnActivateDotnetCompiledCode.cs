using Hashlink.Reflection.Members;
using ModCore.Events;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Hashlink.Events.Interfaces
{
    [Event]
    internal interface IOnActivateDotnetCompiledCode
    {
        public record class Data( HashlinkFunction Function, MethodInfo Method, nint Target );

        void OnActivateDotnetCompiledCode( Data data );
    }
}
