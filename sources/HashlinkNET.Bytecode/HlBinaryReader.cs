using System.Runtime.CompilerServices;
using System.Text;
using static HashlinkNET.Bytecode.HlTypeObj;

namespace HashlinkNET.Bytecode;

public unsafe ref struct HlBinaryReader
{
    private int position;
    private readonly ReadOnlySpan<byte> bytes;
    private readonly HlCode code;

    public HlBinaryReader( ReadOnlySpan<byte> bytes, HlCode code )
    {
        this.bytes = bytes;
        this.code = code;
    }

    internal T Read<T>() where T : unmanaged
    {
        var size = Unsafe.SizeOf<T>();
        if (position + size > bytes.Length)
        {
            throw new IndexOutOfRangeException();
        }
        fixed (byte* data = bytes)
        {
            var value = *((T*)(data + position));
            position += size;
            return value;
        }
    }

    public byte ReadByte()
    {
        return bytes[position++];
    }

    public ReadOnlySpan<byte> ReadBytes( int count )
    {
        if (position + count > bytes.Length)
        {
            throw new IndexOutOfRangeException();
        }

        var value = bytes.Slice(position, count);
        position += count;
        return value;
    }

    public int ReadIndex()
    {
        var b = ReadByte();
        if ((b & 0x80) == 0)
        {
            return b & 0x7f;
        }

        if ((b & 0x40) == 0)
        {
            var v = ReadByte() | ((b & 31) << 8);
            return (b & 0x20) == 0 ? v : -v;
        }
        else
        {
            var c = ReadByte();
            var d = ReadByte();
            var e = ReadByte();
            var v = ((b & 31) << 24) | (c << 16) | (d << 8) | e;
            return (b & 0x20) == 0 ? v : -v;
        }
    }

    public int ReadUIndex()
    {
        var i = ReadIndex();

        if (i >= 0)
        {
            return i;
        }

        Console.WriteLine("Warning: negative index");
        return 0;
    }

    public int ReadInt32()
    {
        return Read<int>();
    }

    public double ReadDouble()
    {
        return Read<double>();
    }

    public List<string> ReadStrings( int count, out List<int> lengths )
    {
        var size = ReadInt32();
        var stringBytes = ReadBytes(size);
        var strings = new List<string>(count);
        lengths = new List<int>(count);

        var offset = 0;

        for (var i = 0; i < count; i++)
        {
            var stringSize = ReadUIndex();
            strings.Add(Encoding.UTF8.GetString(stringBytes.Slice(offset, stringSize)));
            lengths.Add(stringSize);
            offset += stringSize + 1;
        }

        return strings;
    }

    public string ReadUString()
    {
        var i = ReadIndex();

        if (i < 0 || i >= code.Strings.Count)
        {
            Console.WriteLine("Warning: invalid string index");
            i = 0;
        }

        return code.GetUString(i);
    }

    public HlType ReadHlType()
    {
        var kind = (HlTypeKind)ReadByte();

        switch (kind)
        {
            case HlTypeKind.Fun:
            case HlTypeKind.Method:
                {
                    var argumentCount = ReadByte();
                    var arguments = new HlTypeRef[argumentCount];
                    for (var i = 0; i < argumentCount; i++)
                    {
                        arguments[i] = code.GetHlTypeRef(ReadIndex());
                    }

                    var fun = new HlTypeFun(
                    arguments: arguments,
                    returnType: code.GetHlTypeRef(ReadIndex())
                );

                    return new HlTypeWithFun(
                        kind: kind,
                        functionDescription: fun
                    );
                }

            case HlTypeKind.Obj:
            case HlTypeKind.Struct:
                {
                    var name = ReadUString();
                    var super = ReadIndex();
                    var global = ReadUIndex();
                    var nFields = ReadUIndex();
                    var nProtos = ReadUIndex();
                    var nBindings = ReadUIndex();
                    var obj = new HlTypeObj(
                    name: name,
                    super: super < 0 ? null : code.GetHlTypeRef(super),
                    globalValue: global,
                    fields: new HlObjField[nFields],
                    protos: new HlObjProto[nProtos],
                    bindings: new BindingData[nBindings]
                );

                    for (var i = 0; i < nFields; i++)
                    {
                        var fieldName = ReadUString();
                        obj.Fields[i] = new HlObjField(
                            name: fieldName,
                            type: code.GetHlTypeRef(ReadIndex()),
                            i
                        );
                    }

                    for (var i = 0; i < nProtos; i++)
                    {
                        var protoName = ReadUString();

                        obj.Protos[i] = new HlObjProto(
                            name: protoName,
                            fIndex: ReadUIndex(),
                            pIndex: ReadIndex()
                        );

                    }

                    for (var i = 0; i < nBindings; i++)
                    {
                        obj.Bindings[i].FieldIndex = ReadUIndex();
                        obj.Bindings[i].FunctionIndex = ReadUIndex();
                    }

                    return new HlTypeWithObj(kind, obj);
                }

            case HlTypeKind.Ref:
                {
                    return new HlTypeWithType(
                        kind: kind,
                        type: code.GetHlTypeRef(ReadIndex())
                    );
                }

            case HlTypeKind.Virtual:
                {
                    var nFields = ReadUIndex();
                    var virt = new HlTypeVirtual(
                    fields: new HlObjField[nFields]
                );

                    for (var i = 0; i < nFields; i++)
                    {
                        var name = ReadUString();
                        virt.Fields[i] = new HlObjField(
                            name: name,
                            type: code.GetHlTypeRef(ReadIndex()),
                            i
                        );
                    }

                    return new HlTypeWithVirtual(
                        kind: kind,
                        @virtual: virt
                    );
                }

            case HlTypeKind.Abstract:
                {
                    return new HlTypeWithAbsName(
                        kind: kind,
                        abstractName: ReadUString()
                    );
                }

            case HlTypeKind.Enum:
                {
                    var @enum = new HlTypeEnum(
                    name: ReadUString(),
                    globalValue: ReadUIndex(),
                    constructs: new HlEnumConstruct[ReadUIndex()]
                );

                    for (var i = 0; i < @enum.Constructs.Length; i++)
                    {
                        var name = ReadUString();
                        var nParams = ReadUIndex();
                        var construct = @enum.Constructs[i] = new HlEnumConstruct(
                        name: name,
                        @params: new HlTypeRef[nParams],
                        offsets: new int[nParams]
                    );

                        for (var j = 0; j < nParams; j++)
                        {
                            construct.Params[j] = code.GetHlTypeRef(ReadIndex());
                        }
                    }

                    return new HlTypeWithEnum(
                        kind: kind,
                        @enum: @enum
                    );
                }

            case HlTypeKind.Null:
            case HlTypeKind.Packed:
                {
                    return new HlTypeWithType(
                        kind: kind,
                        type: code.GetHlTypeRef(ReadIndex())
                    );
                }

            default:
                {
                    return kind >= HlTypeKind.Last
                        ? throw new InvalidDataException($"Invalid type kind: {kind}")
                        : new HlType(
                    kind: kind
                );
                }
        }
    }

    public HlFunction ReadHlFunction()
    {
        var type = code.GetHlTypeRef(ReadIndex());
        var fIndex = ReadUIndex();
        var nRegs = ReadUIndex();
        var nOps = ReadUIndex();
        var regs = new HlTypeRef[nRegs];
        for (var i = 0; i < nRegs; i++)
        {
            regs[i] = code.GetHlTypeRef(ReadIndex());
        }

        var opcodes = new HlOpcode[nOps];
        var opcodesData = new List<int>(nOps);
        var ctx = new HlOpcode.OpcodeContext();
        for (var i = 0; i < nOps; i++)
        {
            opcodes[i] = ReadHlOpcode(ctx, opcodesData);
        }
        ctx.Data = [.. opcodesData];

        return new HlFunction(
            functionIndex: fIndex,
            type: type,
            localVariables: regs,
            opcodes: opcodes
        );
    }
    private readonly HlOpcode CreateOpcode( HlOpcode.OpcodeContext ctx, List<int> data, params ReadOnlySpan<int> intData )
    {
        var start = data.Count - 1;
        data.EnsureCapacity(data.Count + intData.Length);
        data.AddRange(intData);
        return new(start, data.Count - start, ctx);
    }
    private HlOpcode ReadHlOpcode(HlOpcode.OpcodeContext ctx, List<int> data)
    {
        var kind = (HlOpcodeKind)ReadUIndex();
        var start = data.Count;
        data.Add((int)kind);

        if (kind >= HlOpcodeKind.Last)
        {
            Console.WriteLine($"Warning: invalid opcode kind: {kind}");
            return new HlOpcode(start, 1, ctx);
        }

        switch (kind.GetArgumentCount())
        {
            case 0:
                {
                    return CreateOpcode(
                        ctx, data
                    );
                }

            case 1:
                {
                    return CreateOpcode(
                        ctx, data, 
                        ReadIndex()
                    );
                }

            case 2:
                {
                    return CreateOpcode(
                        ctx, data,
                        ReadIndex(),
                        ReadIndex()
                    );
                }

            case 3:
                {
                    return CreateOpcode(
                        ctx, data,
                        ReadIndex(),
                        ReadIndex(),
                        ReadIndex()
                    );
                }

            case 4:
                {
                    return CreateOpcode(
                        ctx, data,
                        ReadIndex(),
                        ReadIndex(),
                        ReadIndex(),
                        ReadIndex()
                    );
                }

            case -1:
                {
                    switch (kind)
                    {
                        case HlOpcodeKind.CallN:
                        case HlOpcodeKind.CallClosure:
                        case HlOpcodeKind.CallMethod:
                        case HlOpcodeKind.CallThis:
                        case HlOpcodeKind.MakeEnum:
                            {
                                var p1 = ReadIndex();
                                var p2 = ReadIndex();
                                var p3 = ReadByte();
                                var extraParams = new int[p3];
                                for (var i = 0; i < p3; i++)
                                {
                                    extraParams[i] = ReadIndex();
                                }

                                return CreateOpcode(ctx, data, [p1, p2, p3, .. extraParams]);
                            }

                        case HlOpcodeKind.Switch:
                            {
                                var p1 = ReadUIndex();
                                var p2 = ReadUIndex();
                                var extraParams = new int[p2];
                                for (var i = 0; i < p2; i++)
                                {
                                    extraParams[i] = ReadUIndex();
                                }

                                var p3 = ReadUIndex();
                                return CreateOpcode(ctx, data, [p1, p2, p3, .. extraParams]);
                            }

                        default:
                            throw new InvalidDataException($"Invalid opcode kind: {kind}");
                    }
                }

            default:
                {
                    var size = kind.GetArgumentCount() - 3;
                    var p1 = ReadIndex();
                    var p2 = ReadIndex();
                    var p3 = ReadIndex();
                    var extraParams = new int[size];
                    for (var i = 0; i < size; i++)
                    {
                        extraParams[i] = ReadIndex();
                    }

                    return CreateOpcode(ctx, data, [p1, p2, p3, ..extraParams]);
                }
        }
    }

    public HlFunDebug[] ReadDebugInfos( int opcodeCount )
    {
        var currFile = -1;
        var currLine = 0;
        var debug = new HlFunDebug[opcodeCount];
        var i = 0;

        while (i < opcodeCount)
        {
            var c = ReadByte();

            if ((c & 1) != 0)
            {
                c >>= 1;
                currFile = (c << 8) | ReadByte();
                if (currFile >= code.DebugFiles.Count)
                {
                    throw new InvalidDataException($"Invalid debug file index: {currFile}");
                }
            }
            else if ((c & 2) != 0)
            {
                var delta = c >> 6;
                var count = (c >> 2) & 15;
                if (i + count > opcodeCount)
                {
                    throw new InvalidDataException($"Invalid debug line count: {count}");
                }

                while (count-- > 0)
                {
                    debug[i] = new(
                        code.DebugFiles[currFile],
                        currLine);

                    i++;
                }

                currLine += delta;
            }
            else if ((c & 4) != 0)
            {
                currLine += c >> 3;
                debug[i] = new(
                        code.DebugFiles[currFile],
                        currLine);
                i++;
            }
            else
            {
                var b2 = ReadByte();
                var b3 = ReadByte();
                currLine = (c >> 3) | (b2 << 5) | (b3 << 13);
                debug[i] = new(
                        code.DebugFiles[currFile],
                        currLine);
                i++;
            }
        }

        return debug;
    }
}
