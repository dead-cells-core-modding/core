namespace HaxeProxy.Runtime.Internals
{
    /// <summary>
    /// Associates a method with its Hashlink function index.
    /// </summary>
    /// <param name="findex">The Hashlink function index.</param>
    [AttributeUsage(AttributeTargets.Method)]
    public class HashlinkFIndexAttribute( int findex ) : Attribute
    {

        /// <summary>
        /// Gets the Hashlink function index.
        /// </summary>
        public int Index {
            get;
        } = findex;
    }
}
