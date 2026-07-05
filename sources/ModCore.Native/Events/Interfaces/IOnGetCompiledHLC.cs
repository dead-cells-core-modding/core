using ModCore.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModCore.Native.Events.Interfaces
{
    [Event(true)]
    internal interface IOnGetCompiledHLC
    {
        public EventResult<nint> OnGetCompiledHLC( ReadOnlySpan<byte> data );
    }
}
