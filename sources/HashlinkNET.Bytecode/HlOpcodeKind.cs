namespace HashlinkNET.Bytecode;

/// <summary>
///     The kind of opcode.
/// </summary>
public enum HlOpcodeKind
{
    Mov,
    Int,
    Float,
    Bool,
    Bytes,
    String,
    Null,

    Add,
    Sub,
    Mul,
    SDiv,
    UDiv,
    SMod,
    UMod,
    Shl,
    SShr,
    UShr,
    And,
    Or,
    Xor,

    Neg,
    Not,
    Incr,
    Decr,

    Call0,
    Call1,
    Call2,
    Call3,
    Call4,
    CallN,
    CallMethod,
    CallThis,
    CallClosure,

    StaticClosure,
    InstanceClosure,
    VirtualClosure,

    GetGlobal,
    SetGlobal,
    Field,
    SetField,
    GetThis,
    SetThis,
    DynGet,
    DynSet,

    JTrue,
    JFalse,
    JNull,
    JNotNull,
    JSLt,
    JSGte,
    JSGt,
    JSLte,
    JULt,
    JUGte,
    JNotLt,
    JNotGte,
    JEq,
    JNotEq,
    JAlways,

    ToDyn,
    ToSFloat,
    ToUFloat,
    ToInt,
    SafeCast,
    UnsafeCast,
    ToVirtual,

    Label,
    Ret,
    Throw,
    Rethrow,
    Switch,
    NullCheck,
    Trap,
    EndTrap,

    GetI8,
    GetI16,
    GetMem,
    GetArray,
    SetI8,
    SetI16,
    SetMem,
    SetArray,

    New,
    ArraySize,
    Type,
    GetType,
    GetTID,

    Ref,
    Unref,
    Setref,

    MakeEnum,
    EnumAlloc,
    EnumIndex,
    EnumField,
    SetEnumField,

    Assert,
    RefData,
    RefOffset,
    Nop,

    Last,
}

public static class HlOpcodeKindExtensions
{
    public static readonly Dictionary<HlOpcodeKind, int> OPCODE_ARGUMENT_COUNTS = new() {
        { HlOpcodeKind.Mov,              2 },
        { HlOpcodeKind.Int,              2 },
        { HlOpcodeKind.Float,            2 },
        { HlOpcodeKind.Bool,             2 },
        { HlOpcodeKind.Bytes,            2 },
        { HlOpcodeKind.String,           2 },
        { HlOpcodeKind.Null,             1 },

        { HlOpcodeKind.Add,              3 },
        { HlOpcodeKind.Sub,              3 },
        { HlOpcodeKind.Mul,              3 },
        { HlOpcodeKind.SDiv,             3 },
        { HlOpcodeKind.UDiv,             3 },
        { HlOpcodeKind.SMod,             3 },
        { HlOpcodeKind.UMod,             3 },
        { HlOpcodeKind.Shl,              3 },
        { HlOpcodeKind.SShr,             3 },
        { HlOpcodeKind.UShr,             3 },
        { HlOpcodeKind.And,              3 },
        { HlOpcodeKind.Or,               3 },
        { HlOpcodeKind.Xor,              3 },

        { HlOpcodeKind.Neg,              2 },
        { HlOpcodeKind.Not,              2 },
        { HlOpcodeKind.Incr,             1 },
        { HlOpcodeKind.Decr,             1 },

        { HlOpcodeKind.Call0,            2 },
        { HlOpcodeKind.Call1,            3 },
        { HlOpcodeKind.Call2,            4 },
        { HlOpcodeKind.Call3,            5 },
        { HlOpcodeKind.Call4,            6 },
        { HlOpcodeKind.CallN,           -1 },
        { HlOpcodeKind.CallMethod,      -1 },
        { HlOpcodeKind.CallThis,        -1 },
        { HlOpcodeKind.CallClosure,     -1 },

        { HlOpcodeKind.StaticClosure,    2 },
        { HlOpcodeKind.InstanceClosure,  3 },
        { HlOpcodeKind.VirtualClosure,   3 },

        { HlOpcodeKind.GetGlobal,        2 },
        { HlOpcodeKind.SetGlobal,        2 },
        { HlOpcodeKind.Field,            3 },
        { HlOpcodeKind.SetField,         3 },
        { HlOpcodeKind.GetThis,          2 },
        { HlOpcodeKind.SetThis,          2 },
        { HlOpcodeKind.DynGet,           3 },
        { HlOpcodeKind.DynSet,           3 },

        { HlOpcodeKind.JTrue,            2 },
        { HlOpcodeKind.JFalse,           2 },
        { HlOpcodeKind.JNull,            2 },
        { HlOpcodeKind.JNotNull,         2 },
        { HlOpcodeKind.JSLt,             3 },
        { HlOpcodeKind.JSGte,            3 },
        { HlOpcodeKind.JSGt,             3 },
        { HlOpcodeKind.JSLte,            3 },
        { HlOpcodeKind.JULt,             3 },
        { HlOpcodeKind.JUGte,            3 },
        { HlOpcodeKind.JNotLt,           3 },
        { HlOpcodeKind.JNotGte,          3 },
        { HlOpcodeKind.JEq,              3 },
        { HlOpcodeKind.JNotEq,           3 },
        { HlOpcodeKind.JAlways,          1 },

        { HlOpcodeKind.ToDyn,            2 },
        { HlOpcodeKind.ToSFloat,         2 },
        { HlOpcodeKind.ToUFloat,         2 },
        { HlOpcodeKind.ToInt,            2 },
        { HlOpcodeKind.SafeCast,         2 },
        { HlOpcodeKind.UnsafeCast,       2 },
        { HlOpcodeKind.ToVirtual,        2 },

        { HlOpcodeKind.Label,            0 },
        { HlOpcodeKind.Ret,              1 },
        { HlOpcodeKind.Throw,            1 },
        { HlOpcodeKind.Rethrow,          1 },
        { HlOpcodeKind.Switch,          -1 },
        { HlOpcodeKind.NullCheck,        1 },
        { HlOpcodeKind.Trap,             2 },
        { HlOpcodeKind.EndTrap,          1 },

        { HlOpcodeKind.GetI8,            3 },
        { HlOpcodeKind.GetI16,           3 },
        { HlOpcodeKind.GetMem,           3 },
        { HlOpcodeKind.GetArray,         3 },
        { HlOpcodeKind.SetI8,            3 },
        { HlOpcodeKind.SetI16,           3 },
        { HlOpcodeKind.SetMem,           3 },
        { HlOpcodeKind.SetArray,         3 },

        { HlOpcodeKind.New,              1 },
        { HlOpcodeKind.ArraySize,        2 },
        { HlOpcodeKind.Type,             2 },
        { HlOpcodeKind.GetType,          2 },
        { HlOpcodeKind.GetTID,           2 },

        { HlOpcodeKind.Ref,              2 },
        { HlOpcodeKind.Unref,            2 },
        { HlOpcodeKind.Setref,           2 },

        { HlOpcodeKind.MakeEnum,       -1 },
        { HlOpcodeKind.EnumAlloc,       2 },
        { HlOpcodeKind.EnumIndex,       2 },
        { HlOpcodeKind.EnumField,       4 },
        { HlOpcodeKind.SetEnumField,    3 },

        { HlOpcodeKind.Assert,           0 },
        { HlOpcodeKind.RefData,          2 },
        { HlOpcodeKind.RefOffset,        3 },
        { HlOpcodeKind.Nop,              0 },

        { HlOpcodeKind.Last,             0 },
    };

    public static readonly int[] OPCODE_ARGUMENT_COUNTS_FAST = new int[(int)HlOpcodeKind.Last + 1];

    static HlOpcodeKindExtensions()
    {
        foreach ((var opcodeKind, var count) in OPCODE_ARGUMENT_COUNTS)
        {
            OPCODE_ARGUMENT_COUNTS_FAST[(int)opcodeKind] = count;
        }
    }

    public static int GetArgumentCount( this HlOpcodeKind opcodeKind )
    {
        return OPCODE_ARGUMENT_COUNTS_FAST[(int)opcodeKind];
    }
}
