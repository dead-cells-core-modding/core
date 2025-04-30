namespace HashlinkNET.Bytecode;

/// <summary>
///     A HashLink constant, which records information for initializing global
///     variables.
/// </summary>
public sealed class HlConstant
{
    /// <summary>
    ///     The index of the global variable in the HashLink binary.
    /// </summary>
    public int GlobalIndex
    {
        get; set;
    }

    /* if global variable type is obj or struct:
     *     global = globals[globalIndex]
     *     for (var i = 0; i < fields.length; i++) {
     *         var index = fields[i];
     *         var type = global.obj.fields[i].type;
     *         i32: val = ints[index]
     *         bool: val = index != 0
     *         f64: val = index != 0 // code.floats[index]
     *         bytes: val = index != 0 // code.getString(index);
     *         type: val = code.types[index]
     *         default: globals_data + globals_indexes[index] // what
     *         
     *     }
     *  else: exception
     */
    /// <summary>
    ///     The index of the value in which to initialize the global variable.
    ///     <br />
    ///     The meaning of the value differs depending on the global variable
    ///     type.
    /// </summary>
    public int[] Fields
    {
        get; set;
    }

    public HlConstant( int globalIndex, int[] fields )
    {
        GlobalIndex = globalIndex;
        Fields = fields;
    }
}
