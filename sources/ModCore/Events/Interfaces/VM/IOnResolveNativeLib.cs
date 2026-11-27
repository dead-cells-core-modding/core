namespace ModCore.Events.Interfaces.VM
{
    /// <summary>
    /// An event is trigged when resolving a native library.
    /// </summary>
    [Event(false)]
    public interface IOnResolveNativeLib
    {
        /// <summary>
        /// Raised when the VM resolves a native library.
        /// </summary>
        /// <param name="name">The name of the library to resolve.</param>
        /// <returns>A handle to the resolved library, or the default value to continue with the default resolution.</returns>
        EventResult<nint> OnResolveNativeLib( string name );
    }
}
