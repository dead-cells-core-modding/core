using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using static Hashlink.HL_vdynamic;
using System.Xml.Linq;
using Hashlink.Reflection.Types;

namespace Hashlink.Reflection.Members
{
    public unsafe class HashlinkMember
    {
        public HashlinkMemberHandle? Handle
        {
            get;
        }
        public void* NativePointer
        {
            get;
        }

        private int? cachedHashedName;
        public virtual int HashedName
        {
            get
            {
                if (string.IsNullOrEmpty(Name))
                {
                    return 0;
                }
                if (cachedHashedName != null)
                {
                    return cachedHashedName.Value;
                }
                fixed (char* pname = Name)
                {
                    cachedHashedName = hl_hash_gen(pname, false);
                    return cachedHashedName.Value;
                }
            }
        }
        public virtual string? Name => null;
        public virtual HashlinkType? DeclaringType => null;
        public HashlinkModule Module
        {
            get;
        }

        public HashlinkMember( HashlinkModule module, void* ptr )
        {
            Handle = module.GetHandle( ptr );
            if (Handle != null)
            {
                if (Handle.Member != null)
                {
                    throw new InvalidOperationException();
                }
                Handle.Member = this;
            }
            Module = module;
            NativePointer = ptr;
        }

        public override string ToString()
        {
            return Name;
        }

        public T GetMemberFrom<T>( void* ptr ) where T : HashlinkMember, IHashlinkMemberGenerator
        {
            return Module.GetMemberFrom<T>(ptr);
        }
        public T GetMemberFrom<T>( void* ptr, Func<HashlinkModule, nint, T> factory ) where T : HashlinkMember
        {
            return Module.GetMemberFrom<T>(ptr, factory);
        }
    }
}
