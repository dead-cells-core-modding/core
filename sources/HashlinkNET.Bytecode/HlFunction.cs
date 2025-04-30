using System.Buffers;
using System.Collections.ObjectModel;

namespace HashlinkNET.Bytecode;

/// <summary>
///     A HashLink function.
/// </summary>
public class HlFunction
{
    /// <summary>
    ///     The index of the function in the HashLink binary.
    /// </summary>
    public int FunctionIndex
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
    ///     The local variables (registers) of the function.
    /// </summary>
    public HlTypeRef[] LocalVariables
    {
        get; set;
    }

    /// <summary>
    ///     The opcodes of the function.
    /// </summary>
    public HlOpcode[] Opcodes
    {
        get; set;
    }

    /// <summary>
    ///     Debug information for the function. The file and line information
    ///     for each instruction.
    /// </summary>
    public HlFunDebug[]? Debug
    {
        get; set;
    }

    public HlFunAssign[]? Assigns
    {
        get; set;
    }

    public HlFunction( int functionIndex, HlTypeRef type, HlTypeRef[] localVariables, HlOpcode[] opcodes )
    {
        FunctionIndex = functionIndex;
        Type = type;
        LocalVariables = localVariables;
        Opcodes = opcodes;
    }
}

/// <summary>
///     An assign which makes up a HashLink function.
/// </summary>
public struct HlFunDebug
{
    public string? FileName
    {
        get; set;
    }
    public int Line
    {
        get; set;
    }
    public HlFunDebug( string? filename, int line )
    {
        FileName = filename;
        Line = line;
    }
}

/// <summary>
///     An assign which makes up a HashLink function.
/// </summary>
public struct HlFunAssign
{
    public string? Name
    {
        get; set;
    }
    public int Index
    {
        get; set;
    }
    public HlFunAssign( string? name, int index )
    {
        Name = name;
        Index = index;
    }
}

/// <summary>
///     An opcode which makes up a HashLink function.
/// </summary>
public readonly struct HlOpcode
{
    public class OpcodeContext
    {
        public int[] Data
        {
            get; set;
        } = [];
    }
    /// <summary>
    ///     The kind of opcode.
    /// </summary>
    public readonly HlOpcodeKind Kind => (HlOpcodeKind)Data[0];

    /// <summary>
    ///     The parameters of the opcode.
    /// </summary>
    public readonly ReadOnlySpan<int> Parameters => Data[1..];
    public readonly OpcodeContext Context => ctx;
    public readonly ReadOnlySpan<int> Data => new(ctx.Data, start, length);

    private readonly int start;
    private readonly int length;

    private readonly OpcodeContext ctx;

    public HlOpcode( int start, int length, OpcodeContext ctx )
    {
        this.start = start;
        this.length = length;
        this.ctx = ctx;
    }
    
    public readonly override string ToString()
    {
        return Kind.ToString();
    }
}
