using Hashlink.Marshaling;
using Hashlink.Reflection.Members;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
