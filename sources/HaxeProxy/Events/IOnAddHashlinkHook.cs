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
    internal interface IOnAddHashlinkHook
    {
        public record class Data(HashlinkFunction Function, Delegate Target);
        public void OnAddHashlinkHook( Data data );
    }
}
