using dc.haxe.ds;
using dc.hl.types;
using Hashlink.Virtuals;
using HaxeProxy.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Utitities
{
    public static class EnumerableUtils
    {
        public static IEnumerable<dynamic> GetEnumerable( this ArrayBase array )
        {
            for (int i = 0; i < array.length; i++)
            {
                yield return array.getDyn(i);
            }
        }
        public static IEnumerable<dynamic> GetEnumerable( this ArrayDyn array )
        {
            for (int i = 0; i < array.get_length(); i++)
            {
                yield return array.getDyn(i);
            }
        }
        public static IEnumerable<dynamic> GetEnumerable( this ArrayAccess array )
        {
            if (array is ArrayBase ab)
                return ab.GetEnumerable();
            else if (array is ArrayDyn dyn)
                return dyn.GetEnumerable();
            else
                throw new NotSupportedException();
        }

        public static IEnumerator<T> GetEnumerator<T>( this virtual_hasNext_next_<HlFunc<T>> iter )
        {
            while (iter.hasNext())
            {
                yield return iter.next();
            }
        }
    }
}
