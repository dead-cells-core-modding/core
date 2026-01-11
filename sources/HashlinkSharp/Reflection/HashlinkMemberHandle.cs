using Hashlink.Reflection.Members;

namespace Hashlink.Reflection
{
    public unsafe class HashlinkMemberHandle
    {

        internal HashlinkMemberHandle( void* ptr )
        {
            NativePointer = ptr;
        }

        public void* NativePointer
        {
            get;
        }
        public HashlinkMember? Member { get; internal set; }

        
    }
}
