using Hashlink.Proxy;
using Hashlink.Proxy.Objects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Haxe.Array
{
    public abstract class HaxeArray( HashlinkObj obj ) : HaxeObjectBase(obj),
        ICollection
    {
        public abstract int Count
        {
            get;
        }

        public virtual bool IsSynchronized => throw new NotImplementedException();

        public virtual object SyncRoot => throw new NotImplementedException();

        public override bool TryGetIndex( GetIndexBinder binder, object[] indexes, out object? result )
        {
            if (indexes.Length != 1)
            {
                result = null;
                return false;
            }
            result = this[(int)indexes[0]];
            return true;
        }
        public override bool TrySetIndex( SetIndexBinder binder, object[] indexes, object? value )
        {
            if (indexes.Length != 1)
            {
                return false;
            }
            this[(int)indexes[0]] = value;
            return true;
        }

        public virtual void CopyTo( System.Array array, int index )
        {
            for (int i = 0; i < Count; i++)
            {
                array.SetValue(this[i], index);
            }
        }

        public virtual IEnumerator GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }

        public abstract object? this[int index] { get; set; }
    }
}
