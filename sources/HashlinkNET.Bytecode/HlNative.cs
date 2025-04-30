namespace HashlinkNET.Bytecode;

/// <summary>
///     A HashLink native function.
/// </summary>
public class HlNative
{
    /// <summary>
    ///     The name of the library in which the native function is defined.
    /// </summary>
    public string Lib
    {
        get; set;
    }

    /// <summary>
    ///     The name of the native function.
    /// </summary>
    public string Name
    {
        get; set;
    }

    /// <summary>
    ///     A reference to the type definition which describes this function.
    /// </summary>
    public HlTypeRef Type
    {
        get; set;
    }

    /// <summary>
    ///     The index of the native function in the HashLink binary.
    /// </summary>
    public int NativeIndex
    {
        get; set;
    }

    public HlNative( string lib, string name, HlTypeRef typeRef, int nativeIndex )
    {
        Lib = lib;
        Name = name;
        Type = typeRef;
        NativeIndex = nativeIndex;
    }
}
