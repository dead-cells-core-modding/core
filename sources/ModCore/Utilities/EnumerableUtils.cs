using dc.hl.types;
using Hashlink.Virtuals;
using HaxeProxy.Runtime;

namespace ModCore.Utilities
{
    /// <summary>
    /// Provides enumeration helpers for Hashlink collection types.
    /// </summary>
    public static class EnumerableUtils
    {
        /// <summary>
        /// Enumerates the elements of a dynamic Hashlink array.
        /// </summary>
        /// <param name="array">The array to enumerate.</param>
        /// <returns>An enumerator over the array elements.</returns>
        public static IEnumerator<dynamic> GetEnumerator( this ArrayBase array )
        {
            for (int i = 0; i < array.length; i++)
            {
                yield return array.getDyn(i);
            }
        }
        /// <summary>
        /// Enumerates the elements of a typed dynamic Hashlink array.
        /// </summary>
        /// <param name="array">The array to enumerate.</param>
        /// <returns>An enumerator over the array elements.</returns>
        public static IEnumerator<dynamic> GetEnumerator( this ArrayDyn array )
        {
            for (int i = 0; i < array.get_length(); i++)
            {
                yield return array.getDyn(i);
            }
        }
        /// <summary>
        /// Enumerates the elements of a Hashlink array through its access interface.
        /// </summary>
        /// <param name="array">The array to enumerate.</param>
        /// <returns>An enumerator over the array elements.</returns>
        /// <exception cref="NotSupportedException">The array type is not supported.</exception>
        public static IEnumerator<dynamic> GetEnumerator( this ArrayAccess array )
        {
            if (array is ArrayBase ab)
                return ab.GetEnumerator();
            else if (array is ArrayDyn dyn)
                return dyn.GetEnumerator();
            else
                throw new NotSupportedException();
        }
        /// <summary>
        /// Enumerates the elements produced by a Hashlink iterator.
        /// </summary>
        /// <typeparam name="T">The type of the iterated elements.</typeparam>
        /// <param name="iter">The iterator to enumerate.</param>
        /// <returns>An enumerator over the iterator elements.</returns>
        public static IEnumerator<T> GetEnumerator<T>( this virtual_hasNext_next_<HlFunc<T>> iter )
        {
            while (iter.hasNext())
            {
                yield return iter.next();
            }
        }
    }
}
