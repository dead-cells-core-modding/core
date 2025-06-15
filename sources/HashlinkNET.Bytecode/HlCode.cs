namespace HashlinkNET.Bytecode;

/// <summary>
///     A HashLink bytecode binary file.
/// </summary>
public sealed class HlCode
{
    private const int min_version = 2;
    private const int max_version = 5;

    /// <summary>
    ///     The binary file format version.
    /// </summary>
    public int Version
    {
        get; set;
    }

    /// <summary>
    ///     The index of the entrypoint function.
    /// </summary>
    public int Entrypoint
    {
        get; set;
    }

    /// <summary>
    ///     Whether this binary file contains debug information.
    /// </summary>
    public bool HasDebug
    {
        get; set;
    }

    /// <summary>
    ///     The unified function indexes, mapping both functions and natives to
    ///     a single set.
    /// </summary>
    public int[] FunctionIndexes
    {
        get; set;
    } = [];

    /// <summary>
    ///     The integer constants in this binary file.
    /// </summary>
    public List<int> Ints { get; set; } = [];

    /// <summary>
    ///     The floating-point constants in this binary file.
    /// </summary>
    public List<double> Floats { get; set; } = [];

    /// <summary>
    ///     The string constants in this binary file.
    /// </summary>
    public List<string> Strings { get; set; } = [];

    /// <summary>
    ///     The lengths of the string constants in this binary file.
    /// </summary>
    public List<int> StringLengths { get; set; } = [];

    /// <summary>
    ///     The byte constants in this binary file.
    /// </summary>
    public List<byte> Bytes { get; set; } = [];

    /// <summary>
    ///     The positions of the byte constants in this binary file.
    /// </summary>
    public List<int> BytePositions { get; set; } = [];

    /// <summary>
    ///     The names of debug files.
    /// </summary>
    public List<string> DebugFiles { get; set; } = [];

    /// <summary>
    ///     The lengths of the names of debug files.
    /// </summary>
    public List<int> DebugFileLengths { get; set; } = [];

    /// <summary>
    ///     The types in this binary file.
    /// </summary>
    public List<HlType> Types { get; set; } = [];

    /// <summary>
    ///     The global variables in this binary file.
    /// </summary>
    public List<HlTypeRef> Globals { get; set; } = [];

    /// <summary>
    ///     The native functions in this binary file.
    /// </summary>
    public List<HlNative> Natives { get; set; } = [];

    /// <summary>
    ///     The functions in this binary file.
    /// </summary>
    public List<HlFunction> Functions { get; set; } = [];

    /// <summary>
    ///     The unified constant indexes, mapping constant to
    ///     a single set.
    /// </summary>
    public int[] ConstantIndexes
    {
        get; set;
    } = [];
    /// <summary>
    ///     The constants in this binary file.
    /// </summary>
    public List<HlConstant> Constants { get; set; } = [];

    /// <summary>
    ///     Retrieves a wide string constant from this binary file.
    /// </summary>
    /// <param name="index">The index of the string constant.</param>
    /// <returns>The wide string constant.</returns>
    public string GetUString( int index )
    {
        if (index < 0 || index >= Strings.Count)
        {
            Console.WriteLine("Invalid string index.");
            index = 0;
        }

        // Official HL VM stores UStrings elsewhere, but we always decode to
        // UTF8 instead of ANSI or whatever... so it's probably fine?
        return Strings[index];
    }

    public HlFunction? GetFunctionById( int index )
    {
        var idx = FunctionIndexes[index];
        if (idx >= Functions.Count)
        {
            return null;
        }
        return Functions[idx];
    }

  

    /// <summary>
    ///     Retrieves a type from this binary file.
    /// </summary>
    /// <param name="index">The index of the type.</param>
    /// <returns>The type.</returns>
    public HlType GetHlType( int index )
    {
        if (index < 0 || index >= Math.Max(Types.Count, Types.Capacity))
        {
            Console.WriteLine("Invalid type index.");
            index = 0;
        }

        return Types[index];
    }

    /// <summary>
    ///     Retrieves a reference to a type from this binary file.
    /// </summary>
    /// <param name="index">The index of the type.</param>
    /// <returns>A reference to the type.</returns>
    public HlTypeRef GetHlTypeRef( int index )
    {
        if (index < 0 || index >= Math.Max(Types.Count, Types.Capacity))
        {
            Console.WriteLine("Invalid type index.");
            index = 0;
        }

        return new HlTypeRef(index, this);
    }

    /// <summary>
    ///     Creates an instance of <see cref="HlCode"/> from the
    ///     <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <returns>An instance of <see cref="HlCode"/>.</returns>
    public static HlCode FromStream( Stream stream )
    {
        var bytes = new byte[stream.Length];
        var read = stream.Read(bytes, 0, bytes.Length);
        return read != bytes.Length ? throw new IOException("Could not read entire stream") : FromBytes(bytes);
    }

    /// <summary>
    ///     Creates an instance of <see cref="HlCode"/> from the
    ///     <paramref name="data"/>.
    /// </summary>
    /// <param name="data">The data to read from.</param>
    /// <returns>An instance of <see cref="HlCode"/>.</returns>
    public static HlCode FromBytes( ReadOnlySpan<byte> data )
    {
        var code = new HlCode();
        var reader = new HlBinaryReader(data, code);

        var hlb = "HLB"u8;
        if (reader.ReadByte() != hlb[0] || reader.ReadByte() != hlb[1] || reader.ReadByte() != hlb[2])
        {
            throw new InvalidDataException("Not a valid HLB file");
        }

        code.Version = reader.ReadByte();
        if (code.Version is < min_version or > max_version)
        {
            throw new InvalidDataException($"Unsupported HLB version {code.Version}");
        }

        var flags = reader.ReadUIndex();
        var intCount = reader.ReadUIndex();
        var floatCount = reader.ReadUIndex();
        var stringCount = reader.ReadUIndex();
        var byteCount = code.Version >= 5 ? reader.ReadUIndex() : 0;
        var typeCount = reader.ReadUIndex();
        var globalCount = reader.ReadUIndex();
        var nativeCount = reader.ReadUIndex();
        var functionCount = reader.ReadUIndex();
        var constantCount = reader.ReadUIndex();
        code.Entrypoint = reader.ReadUIndex();
        code.HasDebug = (flags & 1) != 0;

        code.Ints = new List<int>(intCount);
        for (var i = 0; i < intCount; i++)
        {
            code.Ints.Add(reader.ReadInt32());
        }

        code.Floats = new List<double>(floatCount);
        for (var i = 0; i < floatCount; i++)
        {
            code.Floats.Add(reader.ReadDouble());
        }

        var strings = reader.ReadStrings(stringCount, out var stringLengths);
        code.Strings = strings;
        code.StringLengths = stringLengths;

        if (code.Version >= 5)
        {
            var size = reader.ReadInt32();
            code.Bytes = reader.ReadBytes(size).ToArray().ToList();
            code.BytePositions = new List<int>(byteCount);
            for (var i = 0; i < byteCount; i++)
            {
                code.BytePositions.Add(reader.ReadUIndex());
            }
        }
        else
        {
            code.Bytes = [];
            code.BytePositions = [];
        }

        var debugFileCount = code.HasDebug ? reader.ReadUIndex() : 0;
        var debugFiles = reader.ReadStrings(debugFileCount, out var debugFileLengths);
        code.DebugFiles = debugFiles;
        code.DebugFileLengths = debugFileLengths;

        code.Types = new List<HlType>(typeCount);
        for (var i = 0; i < typeCount; i++)
        {
            var t = reader.ReadHlType();
            t.TypeIndex = i;
            code.Types.Add(t);
        }

        code.Globals = new List<HlTypeRef>(globalCount);
        for (var i = 0; i < globalCount; i++)
        {
            code.Globals.Add(code.GetHlTypeRef(reader.ReadIndex()));
        }

        code.FunctionIndexes = new int[functionCount + nativeCount];

        code.Natives = new List<HlNative>(nativeCount);

        for (var i = 0; i < nativeCount; i++)
        {
            var native = new HlNative(
                    // In the hashlink source, these use hl_read_string instead, but
                    // we don't make a distinction between strings and ustrings, so
                    // this shouldn't be a concern for us.
                    lib: code.GetUString(reader.ReadIndex()),
                    name: code.GetUString(reader.ReadIndex()),
                    typeRef: code.GetHlTypeRef(reader.ReadIndex()),
                    nativeIndex: reader.ReadUIndex()
                );
            code.FunctionIndexes[native.NativeIndex] = i + functionCount;
            code.Natives.Add(
               native
            );
        }

        code.Functions = new List<HlFunction>(functionCount);

        for (var i = 0; i < functionCount; i++)
        {
            HlFunction func;
            code.Functions.Add(func = reader.ReadHlFunction());
            code.FunctionIndexes[func.FunctionIndex] = i;
            if (!code.HasDebug)
            {
                continue;
            }

            //_ = reader.ReadDebugInfos(func.Opcodes.Length);
            func.Debug = reader.ReadDebugInfos(func.Opcodes.Length);

            if (code.Version < 3)
            {
                continue;
            }


            var nAssigns = reader.ReadUIndex();

            func.Assigns = new HlFunAssign[nAssigns];


            for (var j = 0; j < nAssigns; j++)
            {
                var name = code.GetUString(reader.ReadUIndex());
                var idx = reader.ReadIndex();
                func.Assigns[j] = new(name, idx);
            }

        }

        code.Constants = new List<HlConstant>(constantCount);
        code.ConstantIndexes = new int[globalCount + 1];

        for (var i = 0; i < constantCount; i++)
        {
            HlConstant constant;
            code.Constants.Add(
                constant = new HlConstant(
                    globalIndex: reader.ReadUIndex(),
                    fields: new int[reader.ReadUIndex()]
                )
            );

            code.ConstantIndexes[constant.GlobalIndex] = i;

            for (var j = 0; j < constant.Fields.Length; j++)
            {
                constant.Fields[j] = reader.ReadUIndex();
            }
        }

        return code;
    }
}
