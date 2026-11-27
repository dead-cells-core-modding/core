namespace HaxeProxy.Runtime.Internals
{
    /// <summary>
    /// Specifies the initial value of a generated field or property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class HashlinkInitialvalueAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HashlinkInitialvalueAttribute"/> class.
        /// </summary>
        /// <param name="value">The initial value.</param>
        public HashlinkInitialvalueAttribute( object value )
        {
            _ = value;
        }
    }
}
