namespace HashlinkNET.Bytecode;

/// <summary>
///     A reference to a HashLink type, which acts as sort of a "promise".
/// </summary>
public sealed class HlTypeRef
{
    private readonly int typeId;
    private readonly HlCode code;

    /// <summary>
    ///     The actual type, if it exists and has been deserialized.
    /// </summary>
    public HlType Value => code.GetHlType(typeId);

    public HlTypeRef( int typeId, HlCode code )
    {
        this.typeId = typeId;
        this.code = code;
    }
}
