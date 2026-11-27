namespace ModCore.Events.Interfaces.VM
{
    /// <summary>
    /// An event is trigged when resolving a native library.
    /// </summary>
    [Event(false)]
    public interface IOnResolveNativeFunction
    {
        /// <summary>
        /// Describes a native function to resolve.
        /// </summary>
        public struct NativeFunctionInfo
        {
            /// <summary>
            /// The name of the native library
            /// </summary>
            public string libname;
            /// <summary>
            /// The name of the function
            /// </summary>
            public string name;
        }
        /// <summary>
        /// Raised when the VM resolves a native function.
        /// </summary>
        /// <param name="info">Information about the function to resolve.</param>
        /// <returns>A pointer to the resolved function, or the default value to continue with the default resolution.</returns>
        EventResult<nint> OnResolveNativeFunction( NativeFunctionInfo info );
    }
}
