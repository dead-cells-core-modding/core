using Hashlink.Proxy.Objects;
using Hashlink.Reflection.Types;
using ModCore.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaxeProxy.Events
{
    [Event]
    public interface IOnRegisterCustomType
    {
        public record class Data(Type Type, HashlinkObjectType ObjectType, HashlinkObjectType TemplateType);
        void OnRegisterCustomType( Data data );
    }
}
