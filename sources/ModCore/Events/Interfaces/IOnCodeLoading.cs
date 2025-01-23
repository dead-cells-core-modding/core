using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Events.Interfaces
{
    [Event(true)]
    public interface IOnCodeLoading
    {
        public void OnCodeLoading(ref Span<byte> data);
    }
}
