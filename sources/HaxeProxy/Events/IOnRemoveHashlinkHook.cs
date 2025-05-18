using Hashlink.Reflection.Members;
using ModCore.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaxeProxy.Events
{
    [Event]
    internal interface IOnRemoveHashlinkHook
    {
        public record class Data(HashlinkFunction Function, Delegate Target);
        public void OnRemoveHashlinkHook( Data data );
    }
}
