using Hashlink.Reflection.Types;
using ModCore.Events;

namespace HaxeProxy.Events
{
    [Event]
    internal interface IOnRegisterCustomType
    {
        public record class Data( Type Type, HashlinkObjectType ObjectType, HashlinkObjectType TemplateType );
        void OnRegisterCustomType( Data data );
    }
}
