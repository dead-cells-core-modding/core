namespace HaxeProxy.Runtime.Internals
{

    /// <summary>
    /// Associates a type with its Hashlink type index.
    /// </summary>
    /// <param name="tindex">The Hashlink type index.</param>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum |
        AttributeTargets.Delegate)]
    public class HashlinkTIndexAttribute( int tindex ) : Attribute
    {

        /// <summary>
        /// Gets the Hashlink type index.
        /// </summary>
        public int Index {
            get;
        } = tindex;
    }
}
