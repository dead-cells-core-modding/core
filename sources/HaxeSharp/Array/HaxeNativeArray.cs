using Hashlink.Proxy.Objects;
using Haxe.Marshaling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Haxe.Array
{
    [Obsolete]
    public class HaxeNativeArray( HashlinkArray array ) : HaxeArray(array)
    {
        public override object? this[int index]
        {
            get
            {
                return HaxeMarshal.PostProcessValue(array[index]);
            }
            set
            {
                array[index] = value;
            }
        }

        public override int Count => array.Count;
    }
}
