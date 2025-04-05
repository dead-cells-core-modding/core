using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Patch.Reader
{
    public abstract class HlOpCodeReader
    {
        public abstract bool IsEmpty
        {
            get;
        }
        public abstract bool MoveNext();
        public abstract int Read( int operandCount );
    }
}
