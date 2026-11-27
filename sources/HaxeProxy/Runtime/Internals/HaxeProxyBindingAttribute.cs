namespace HaxeProxy.Runtime.Internals
{
    /// <summary>
    /// Binds a Hashlink type index to its managed proxy type.
    /// </summary>
    /// <param name="typeIndex">The Hashlink type index.</param>
    /// <param name="type">The managed proxy type.</param>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class HaxeProxyBindingAttribute( int typeIndex, Type type ) : Attribute
    {
        /// <summary>
        /// Gets the Hashlink type index.
        /// </summary>
        public int TypeIndex => typeIndex;
        /// <summary>
        /// Gets the managed proxy type.
        /// </summary>
        public Type Type => type;

        /// <summary>
        /// Computes the type identifier of a sub-type, such as an enum constructor.
        /// </summary>
        /// <param name="typeIndex">The parent Hashlink type index.</param>
        /// <param name="subIndex">The sub-type index.</param>
        /// <returns>The combined sub-type identifier.</returns>
        public static int GetSubTypeId( int typeIndex, int subIndex )
        {
            return (int)(0x80000000 | ((uint)typeIndex << 16) | (uint)subIndex);
        }
    }
}
