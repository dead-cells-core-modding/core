using ModCore.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModCore.Native.Events.Interfaces
{
    [Event]
    internal interface IOnResolveHLCSymbol
    {
        public record class Data( nint IP, int FunctionIndex );
        public EventResult<string?> OnResolveHLCSymbol( Data ev );
    }
}
