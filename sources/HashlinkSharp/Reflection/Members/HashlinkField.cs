using Hashlink.Proxy;
using Hashlink.Proxy.Objects;
using Hashlink.Reflection.Types;

namespace Hashlink.Reflection.Members
{
    public unsafe abstract class HashlinkField( HashlinkModule module, void* ptr ) : HashlinkMember(module, ptr),
        IHashlinkMemberGenerator
    {
        public abstract HashlinkType FieldType {
            get;
        }
        public object? GetValue( HashlinkObj obj )
        {
            return ((IHashlinkFieldObject)obj).GetFieldValue(HashedName);
        }
        public void SetValue( HashlinkObj obj, object? value )
        {
            ((IHashlinkFieldObject)obj).SetFieldValue(HashedName, value);
        }

        static HashlinkMember IHashlinkMemberGenerator.GenerateFromPointer( HashlinkModule module, void* ptr )
        {
            throw new NotImplementedException();
        }
    }
}
