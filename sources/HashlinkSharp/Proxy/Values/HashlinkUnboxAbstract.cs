using Hashlink.Reflection.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Proxy.Values
{
    public unsafe class HashlinkUnboxAbstract( HashlinkAbstractType type, void* target ) : IHashlinkPointer
    {
        public HashlinkAbstractType Type => type;

        public void* Value
        {
            get; set;
        } = target;

        nint IHashlinkPointer.HashlinkPointer => (nint)Value;
        public override string? ToString()
        {
            return Type.ToString();
        }
    }
}
