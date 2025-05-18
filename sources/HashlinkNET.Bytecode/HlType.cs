using System.Text;

namespace HashlinkNET.Bytecode;

public enum HlTypeKind
{
    Void = 0,
    UI8 = 1,
    UI16 = 2,
    I32 = 3,
    I64 = 4,
    F32 = 5,
    F64 = 6,
    Bool = 7,
    Bytes = 8,
    Dyn = 9,
    Fun = 10,
    Obj = 11,
    Array = 12,
    Type = 13,
    Ref = 14,
    Virtual = 15,
    DynObj = 16,
    Abstract = 17,
    Enum = 18,
    Null = 19,
    Method = 20,
    Struct = 21,
    Packed = 22,

    Last = 23,
}

/// <summary>
///     A HashLink type.
/// </summary>
public class HlType
{
    /// <summary>
    ///     The kind of type.
    /// </summary>
    public HlTypeKind Kind
    {
        get; set;
    }
    /// <summary>
    ///     The index of type.
    /// </summary>
    public int TypeIndex
    {
        get; set;
    }
    public virtual string GetShortName()
    {
        return Kind.ToString();
    }

    public override string ToString()
    {
        return GetShortName();
    }

    public HlType( HlTypeKind kind )
    {
        Kind = kind;
    }
}

/// <summary>
///     A HashLink abstract type, which consists of a
///     <see cref="AbstractName"/>.
/// </summary>
public sealed class HlTypeWithAbsName : HlType, IHlNamedType
{
    /// <summary>
    ///     The name of the abstract.
    /// </summary>
    public string AbstractName
    {
        get; set;
    }

    public string Name => AbstractName;

    public HlTypeWithAbsName( HlTypeKind kind, string abstractName ) : base(kind)
    {
        AbstractName = abstractName;
    }
}

/// <summary>
///     The description of a function.
/// </summary>
public class HlTypeFun
{
    /// <summary>
    ///     The arguments of the function.
    /// </summary>
    public HlTypeRef[] Arguments
    {
        get; set;
    }

    /// <summary>
    ///     The return type of the function.
    /// </summary>
    public HlTypeRef ReturnType
    {
        get; set;
    }

    public HlTypeFun( HlTypeRef[] arguments, HlTypeRef returnType )
    {
        Arguments = arguments;
        ReturnType = returnType;
    }
}

/// <summary>
///     A HashLink type which describes a function.
/// </summary>
public sealed class HlTypeWithFun : HlType
{
    /// <summary>
    ///     The function description.
    /// </summary>
    public HlTypeFun FunctionDescription
    {
        get; set;
    }

    public override string GetShortName()
    {
        var sb = new StringBuilder();
        sb.Append("Fun_");
        sb.Append(FunctionDescription.ReturnType.Value.GetShortName());
        foreach (var v in FunctionDescription.Arguments)
        {
            sb.Append('_');
            sb.Append(v.Value.GetShortName());
        }
        return sb.ToString();
    }
    public HlTypeWithFun( HlTypeKind kind, HlTypeFun functionDescription ) : base(kind)
    {
        FunctionDescription = functionDescription;
    }
}

public class HlObjField
{
    public string Name
    {
        get; set;
    }

    public HlTypeRef Type
    {
        get; set;
    }
    public int Index
    {
        get; set;
    }
    public HlObjField( string name, HlTypeRef type, int index )
    {
        Name = name;
        Type = type;
        Index = index;
    }
}

public class HlObjProto
{
    public string Name
    {
        get; set;
    }

    public int FIndex
    {
        get; set;
    }

    public int PIndex
    {
        get; set;
    }

    public HlObjProto( string name, int fIndex, int pIndex )
    {
        Name = name;
        FIndex = fIndex;
        PIndex = pIndex;
    }
}

/// <summary>
///     A description of an object (comparable to a class).
/// </summary>
public class HlTypeObj
{
    /// <summary>
    ///     The name of the object.
    /// </summary>
    public string Name
    {
        get; set;
    }

    /// <summary>
    ///     The type being inherited, if applicable.
    /// </summary>
    public HlTypeRef? Super
    {
        get; set;
    }

    /// <summary>
    ///     The fields of the object.
    /// </summary>
    public HlObjField[] Fields
    {
        get; set;
    }

    /// <summary>
    ///     The prototypes of the object.
    /// </summary>
    public HlObjProto[] Protos
    {
        get; set;
    }

    public int[] Bindings
    {
        get; set;
    }

    public int GlobalValue
    {
        get; set;
    }

    public HlTypeObj( string name, HlTypeRef? super, HlObjField[] fields, HlObjProto[] protos, int[] bindings, int globalValue )
    {
        Name = name;
        Super = super;
        Fields = fields;
        Protos = protos;
        Bindings = bindings;
        GlobalValue = globalValue;
    }
}

/// <summary>
///     A HashLink type which describes an object (comparable to a class).
/// </summary>
public sealed class HlTypeWithObj : HlType, IHlNamedType
{
    /// <summary>
    ///     The object description.
    /// </summary>
    public HlTypeObj Obj
    {
        get; set;
    }

    public string Name => Obj.Name;

    public override string ToString()
    {
        return Name;
    }

    public override string GetShortName()
    {
        var lastDot = Obj.Name.LastIndexOf('.');
        if (lastDot < 0)
        {
            return Obj.Name;
        }
        return Obj.Name[(lastDot + 1)..];
    }

    public HlTypeWithObj( HlTypeKind kind, HlTypeObj obj ) : base(kind)
    {
        Obj = obj;
    }
}

public class HlEnumConstruct
{
    public string Name
    {
        get; set;
    }

    public HlTypeRef[] Params
    {
        get; set;
    }

    public int[] Offsets
    {
        get; set;
    }

    public HlEnumConstruct( string name, HlTypeRef[] @params, int[] offsets )
    {
        Name = name;
        Params = @params;
        Offsets = offsets;
    }
}

public class HlTypeEnum
{
    public string Name
    {
        get; set;
    }

    public HlEnumConstruct[] Constructs
    {
        get; set;
    }

    public int GlobalValue
    {
        get; set;
    }

    public HlTypeEnum( string name, HlEnumConstruct[] constructs, int globalValue )
    {
        Name = name;
        Constructs = constructs;
        GlobalValue = globalValue;
    }
}

public sealed class HlTypeWithEnum : HlType, IHlNamedType
{
    public HlTypeEnum Enum
    {
        get; set;
    }

    public string Name => Enum.Name;

    public HlTypeWithEnum( HlTypeKind kind, HlTypeEnum @enum ) : base(kind)
    {
        Enum = @enum;
    }
}

public class HlTypeVirtual
{
    public HlObjField[] Fields
    {
        get; set;
    }

    public HlTypeVirtual( HlObjField[] fields )
    {
        Fields = fields;
    }
}

public sealed class HlTypeWithVirtual : HlType
{
    private void AppendName( StringBuilder sb, HashSet<HlType> looked )
    {
        if (!looked.Add(this))
        {
            sb.Append("__Loop_Ref__");
            return;
        }
        sb.Append($"_virtual_");
        foreach (var v in Virtual.Fields)
        {
            sb.Append(v.Name);
            sb.Append('_');
            if (v.Type.Value is HlTypeWithVirtual virt)
            {
                virt.AppendName(sb, looked);
            }
            else
            {
                sb.Append(v.Type.Value);
            }
            sb.Append('_');
        }
    }
    public override string ToString()
    {
        var sb = new StringBuilder();
        AppendName(sb, []);
        return sb.ToString();
    }
    public HlTypeVirtual Virtual
    {
        get; set;
    }

    public HlTypeWithVirtual( HlTypeKind kind, HlTypeVirtual @virtual ) : base(kind)
    {
        Virtual = @virtual;
    }
}

public sealed class HlTypeWithType : HlType
{
    public HlTypeRef Type
    {
        get; set;
    }

    public HlTypeWithType( HlTypeKind kind, HlTypeRef type ) : base(kind)
    {
        Type = type;
    }
}
