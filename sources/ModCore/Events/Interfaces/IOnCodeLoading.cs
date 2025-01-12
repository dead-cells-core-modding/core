using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Events.Interfaces
{
    public interface IOnCodeLoading : ICallOnceEvent<IOnCodeLoading>
    {
        public void OnCodeLoading(ref Span<byte> data);
    }
}
