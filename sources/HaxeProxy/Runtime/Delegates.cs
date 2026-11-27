namespace HaxeProxy.Runtime
{
    /// <summary>
    /// Represents a Hashlink function that returns a value.
    /// </summary>
    /// <typeparam name="TRet">The return type.</typeparam>
    public delegate TRet HlFunc<TRet>()
        where TRet : allows ref struct
        ;
    /// <summary>
    /// Represents a Hashlink function that takes one argument and returns a value.
    /// </summary>
    /// <typeparam name="TRet">The return type.</typeparam>
    /// <typeparam name="TArg1">The type of the first argument.</typeparam>
    /// <param name="arg1">The first argument.</param>
    public delegate TRet HlFunc<TRet, TArg1>( TArg1 arg1 )
        where TRet : allows ref struct
        where TArg1 : allows ref struct
        ;
    /// <summary>
    /// Represents a Hashlink function that takes two arguments and returns a value.
    /// </summary>
    /// <typeparam name="TRet">The return type.</typeparam>
    /// <typeparam name="TArg1">The type of the first argument.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument.</typeparam>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    public delegate TRet HlFunc<TRet, TArg1, TArg2>( TArg1 arg1, TArg2 arg2 )
        where TRet : allows ref struct
        where TArg1 : allows ref struct
        where TArg2 : allows ref struct
        ;
    /// <summary>
    /// Represents a Hashlink function that takes three arguments and returns a value.
    /// </summary>
    /// <typeparam name="TRet">The return type.</typeparam>
    /// <typeparam name="TArg1">The type of the first argument.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument.</typeparam>
    /// <typeparam name="TArg3">The type of the third argument.</typeparam>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="arg3">The third argument.</param>
    public delegate TRet HlFunc<TRet, TArg1, TArg2, TArg3>( TArg1 arg1, TArg2 arg2, TArg3 arg3 )
        where TRet : allows ref struct
        where TArg1 : allows ref struct
        where TArg2 : allows ref struct
        where TArg3 : allows ref struct
        ;
    /// <summary>
    /// Represents a Hashlink function that takes four arguments and returns a value.
    /// </summary>
    /// <typeparam name="TRet">The return type.</typeparam>
    /// <typeparam name="TArg1">The type of the first argument.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument.</typeparam>
    /// <typeparam name="TArg3">The type of the third argument.</typeparam>
    /// <typeparam name="TArg4">The type of the fourth argument.</typeparam>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="arg3">The third argument.</param>
    /// <param name="arg4">The fourth argument.</param>
    public delegate TRet HlFunc<TRet, TArg1, TArg2, TArg3, TArg4>( TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4 )
        where TRet : allows ref struct
        where TArg1 : allows ref struct
        where TArg2 : allows ref struct
        where TArg3 : allows ref struct
        where TArg4 : allows ref struct
        ;
    /// <summary>
    /// Represents a Hashlink function that takes five arguments and returns a value.
    /// </summary>
    /// <typeparam name="TRet">The return type.</typeparam>
    /// <typeparam name="TArg1">The type of the first argument.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument.</typeparam>
    /// <typeparam name="TArg3">The type of the third argument.</typeparam>
    /// <typeparam name="TArg4">The type of the fourth argument.</typeparam>
    /// <typeparam name="TArg5">The type of the fifth argument.</typeparam>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="arg3">The third argument.</param>
    /// <param name="arg4">The fourth argument.</param>
    /// <param name="arg5">The fifth argument.</param>
    public delegate TRet HlFunc<TRet, TArg1, TArg2, TArg3, TArg4, TArg5>( TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5 )
        where TRet : allows ref struct
        where TArg1 : allows ref struct
        where TArg2 : allows ref struct
        where TArg3 : allows ref struct
        where TArg4 : allows ref struct
        where TArg5 : allows ref struct
        ;
    /// <summary>
    /// Represents a Hashlink function that takes six arguments and returns a value.
    /// </summary>
    /// <typeparam name="TRet">The return type.</typeparam>
    /// <typeparam name="TArg1">The type of the first argument.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument.</typeparam>
    /// <typeparam name="TArg3">The type of the third argument.</typeparam>
    /// <typeparam name="TArg4">The type of the fourth argument.</typeparam>
    /// <typeparam name="TArg5">The type of the fifth argument.</typeparam>
    /// <typeparam name="TArg6">The type of the sixth argument.</typeparam>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="arg3">The third argument.</param>
    /// <param name="arg4">The fourth argument.</param>
    /// <param name="arg5">The fifth argument.</param>
    /// <param name="arg6">The sixth argument.</param>
    public delegate TRet HlFunc<TRet, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>( TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6 )
        where TRet : allows ref struct
        where TArg1 : allows ref struct
        where TArg2 : allows ref struct
        where TArg3 : allows ref struct
        where TArg4 : allows ref struct
        where TArg5 : allows ref struct
        where TArg6 : allows ref struct
        ;
    /// <summary>
    /// Represents a Hashlink function that takes seven arguments and returns a value.
    /// </summary>
    /// <typeparam name="TRet">The return type.</typeparam>
    /// <typeparam name="TArg1">The type of the first argument.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument.</typeparam>
    /// <typeparam name="TArg3">The type of the third argument.</typeparam>
    /// <typeparam name="TArg4">The type of the fourth argument.</typeparam>
    /// <typeparam name="TArg5">The type of the fifth argument.</typeparam>
    /// <typeparam name="TArg6">The type of the sixth argument.</typeparam>
    /// <typeparam name="TArg7">The type of the seventh argument.</typeparam>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="arg3">The third argument.</param>
    /// <param name="arg4">The fourth argument.</param>
    /// <param name="arg5">The fifth argument.</param>
    /// <param name="arg6">The sixth argument.</param>
    /// <param name="arg7">The seventh argument.</param>
    public delegate TRet HlFunc<TRet, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>( TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7 )
        where TRet : allows ref struct
        where TArg1 : allows ref struct
        where TArg2 : allows ref struct
        where TArg3 : allows ref struct
        where TArg4 : allows ref struct
        where TArg5 : allows ref struct
        where TArg6 : allows ref struct
        where TArg7 : allows ref struct
        ;
    /// <summary>
    /// Represents a Hashlink function that takes eight arguments and returns a value.
    /// </summary>
    /// <typeparam name="TRet">The return type.</typeparam>
    /// <typeparam name="TArg1">The type of the first argument.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument.</typeparam>
    /// <typeparam name="TArg3">The type of the third argument.</typeparam>
    /// <typeparam name="TArg4">The type of the fourth argument.</typeparam>
    /// <typeparam name="TArg5">The type of the fifth argument.</typeparam>
    /// <typeparam name="TArg6">The type of the sixth argument.</typeparam>
    /// <typeparam name="TArg7">The type of the seventh argument.</typeparam>
    /// <typeparam name="TArg8">The type of the eighth argument.</typeparam>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="arg3">The third argument.</param>
    /// <param name="arg4">The fourth argument.</param>
    /// <param name="arg5">The fifth argument.</param>
    /// <param name="arg6">The sixth argument.</param>
    /// <param name="arg7">The seventh argument.</param>
    /// <param name="arg8">The eighth argument.</param>
    public delegate TRet HlFunc<TRet, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8>( TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8 )
        where TRet : allows ref struct
        where TArg1 : allows ref struct
        where TArg2 : allows ref struct
        where TArg3 : allows ref struct
        where TArg4 : allows ref struct
        where TArg5 : allows ref struct
        where TArg6 : allows ref struct
        where TArg7 : allows ref struct
        where TArg8 : allows ref struct
        ;
    /// <summary>
    /// Represents a Hashlink function that takes nine arguments and returns a value.
    /// </summary>
    /// <typeparam name="TRet">The return type.</typeparam>
    /// <typeparam name="TArg1">The type of the first argument.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument.</typeparam>
    /// <typeparam name="TArg3">The type of the third argument.</typeparam>
    /// <typeparam name="TArg4">The type of the fourth argument.</typeparam>
    /// <typeparam name="TArg5">The type of the fifth argument.</typeparam>
    /// <typeparam name="TArg6">The type of the sixth argument.</typeparam>
    /// <typeparam name="TArg7">The type of the seventh argument.</typeparam>
    /// <typeparam name="TArg8">The type of the eighth argument.</typeparam>
    /// <typeparam name="TArg9">The type of the ninth argument.</typeparam>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="arg3">The third argument.</param>
    /// <param name="arg4">The fourth argument.</param>
    /// <param name="arg5">The fifth argument.</param>
    /// <param name="arg6">The sixth argument.</param>
    /// <param name="arg7">The seventh argument.</param>
    /// <param name="arg8">The eighth argument.</param>
    /// <param name="arg9">The ninth argument.</param>
    public delegate TRet HlFunc<TRet, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9>( TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9 )
        where TRet : allows ref struct
        where TArg1 : allows ref struct
        where TArg2 : allows ref struct
        where TArg3 : allows ref struct
        where TArg4 : allows ref struct
        where TArg5 : allows ref struct
        where TArg6 : allows ref struct
        where TArg7 : allows ref struct
        where TArg8 : allows ref struct
        where TArg9 : allows ref struct
        ;
    /// <summary>
    /// Represents a Hashlink function that takes ten arguments and returns a value.
    /// </summary>
    /// <typeparam name="TRet">The return type.</typeparam>
    /// <typeparam name="TArg1">The type of the first argument.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument.</typeparam>
    /// <typeparam name="TArg3">The type of the third argument.</typeparam>
    /// <typeparam name="TArg4">The type of the fourth argument.</typeparam>
    /// <typeparam name="TArg5">The type of the fifth argument.</typeparam>
    /// <typeparam name="TArg6">The type of the sixth argument.</typeparam>
    /// <typeparam name="TArg7">The type of the seventh argument.</typeparam>
    /// <typeparam name="TArg8">The type of the eighth argument.</typeparam>
    /// <typeparam name="TArg9">The type of the ninth argument.</typeparam>
    /// <typeparam name="TArg10">The type of the tenth argument.</typeparam>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="arg3">The third argument.</param>
    /// <param name="arg4">The fourth argument.</param>
    /// <param name="arg5">The fifth argument.</param>
    /// <param name="arg6">The sixth argument.</param>
    /// <param name="arg7">The seventh argument.</param>
    /// <param name="arg8">The eighth argument.</param>
    /// <param name="arg9">The ninth argument.</param>
    /// <param name="arg10">The tenth argument.</param>
    public delegate TRet HlFunc<TRet, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10>( TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10 )
        where TRet : allows ref struct
        where TArg1 : allows ref struct
        where TArg2 : allows ref struct
        where TArg3 : allows ref struct
        where TArg4 : allows ref struct
        where TArg5 : allows ref struct
        where TArg6 : allows ref struct
        where TArg7 : allows ref struct
        where TArg8 : allows ref struct
        where TArg9 : allows ref struct
        where TArg10 : allows ref struct
        ;

    /// <summary>
    /// Represents a Hashlink function that takes no arguments and returns no value.
    /// </summary>
    public delegate void HlAction();
    /// <summary>
    /// Represents a Hashlink function that takes one argument and returns no value.
    /// </summary>
    /// <typeparam name="TArg1">The type of the first argument.</typeparam>
    /// <param name="arg1">The first argument.</param>
    public delegate void HlAction<TArg1>( TArg1 arg1 )
        where TArg1 : allows ref struct
        ;
    /// <summary>
    /// Represents a Hashlink function that takes two arguments and returns no value.
    /// </summary>
    /// <typeparam name="TArg1">The type of the first argument.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument.</typeparam>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    public delegate void HlAction<TArg1, TArg2>( TArg1 arg1, TArg2 arg2 )
        where TArg1 : allows ref struct
        where TArg2 : allows ref struct
        ;
    /// <summary>
    /// Represents a Hashlink function that takes three arguments and returns no value.
    /// </summary>
    /// <typeparam name="TArg1">The type of the first argument.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument.</typeparam>
    /// <typeparam name="TArg3">The type of the third argument.</typeparam>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="arg3">The third argument.</param>
    public delegate void HlAction<TArg1, TArg2, TArg3>( TArg1 arg1, TArg2 arg2, TArg3 arg3 )
        where TArg1 : allows ref struct
        where TArg2 : allows ref struct
        where TArg3 : allows ref struct
        ;
    /// <summary>
    /// Represents a Hashlink function that takes four arguments and returns no value.
    /// </summary>
    /// <typeparam name="TArg1">The type of the first argument.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument.</typeparam>
    /// <typeparam name="TArg3">The type of the third argument.</typeparam>
    /// <typeparam name="TArg4">The type of the fourth argument.</typeparam>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="arg3">The third argument.</param>
    /// <param name="arg4">The fourth argument.</param>
    public delegate void HlAction<TArg1, TArg2, TArg3, TArg4>( TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4 )
        where TArg1 : allows ref struct
        where TArg2 : allows ref struct
        where TArg3 : allows ref struct
        where TArg4 : allows ref struct
        ;
    /// <summary>
    /// Represents a Hashlink function that takes five arguments and returns no value.
    /// </summary>
    /// <typeparam name="TArg1">The type of the first argument.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument.</typeparam>
    /// <typeparam name="TArg3">The type of the third argument.</typeparam>
    /// <typeparam name="TArg4">The type of the fourth argument.</typeparam>
    /// <typeparam name="TArg5">The type of the fifth argument.</typeparam>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="arg3">The third argument.</param>
    /// <param name="arg4">The fourth argument.</param>
    /// <param name="arg5">The fifth argument.</param>
    public delegate void HlAction<TArg1, TArg2, TArg3, TArg4, TArg5>( TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5 )
        where TArg1 : allows ref struct
        where TArg2 : allows ref struct
        where TArg3 : allows ref struct
        where TArg4 : allows ref struct
        where TArg5 : allows ref struct
        ;
    /// <summary>
    /// Represents a Hashlink function that takes six arguments and returns no value.
    /// </summary>
    /// <typeparam name="TArg1">The type of the first argument.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument.</typeparam>
    /// <typeparam name="TArg3">The type of the third argument.</typeparam>
    /// <typeparam name="TArg4">The type of the fourth argument.</typeparam>
    /// <typeparam name="TArg5">The type of the fifth argument.</typeparam>
    /// <typeparam name="TArg6">The type of the sixth argument.</typeparam>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="arg3">The third argument.</param>
    /// <param name="arg4">The fourth argument.</param>
    /// <param name="arg5">The fifth argument.</param>
    /// <param name="arg6">The sixth argument.</param>
    public delegate void HlAction<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>( TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6 )
        where TArg1 : allows ref struct
        where TArg2 : allows ref struct
        where TArg3 : allows ref struct
        where TArg4 : allows ref struct
        where TArg5 : allows ref struct
        where TArg6 : allows ref struct
        ;
    /// <summary>
    /// Represents a Hashlink function that takes seven arguments and returns no value.
    /// </summary>
    /// <typeparam name="TArg1">The type of the first argument.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument.</typeparam>
    /// <typeparam name="TArg3">The type of the third argument.</typeparam>
    /// <typeparam name="TArg4">The type of the fourth argument.</typeparam>
    /// <typeparam name="TArg5">The type of the fifth argument.</typeparam>
    /// <typeparam name="TArg6">The type of the sixth argument.</typeparam>
    /// <typeparam name="TArg7">The type of the seventh argument.</typeparam>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="arg3">The third argument.</param>
    /// <param name="arg4">The fourth argument.</param>
    /// <param name="arg5">The fifth argument.</param>
    /// <param name="arg6">The sixth argument.</param>
    /// <param name="arg7">The seventh argument.</param>
    public delegate void HlAction<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>( TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7 )
        where TArg1 : allows ref struct
        where TArg2 : allows ref struct
        where TArg3 : allows ref struct
        where TArg4 : allows ref struct
        where TArg5 : allows ref struct
        where TArg6 : allows ref struct
        where TArg7 : allows ref struct
        ;
    /// <summary>
    /// Represents a Hashlink function that takes eight arguments and returns no value.
    /// </summary>
    /// <typeparam name="TArg1">The type of the first argument.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument.</typeparam>
    /// <typeparam name="TArg3">The type of the third argument.</typeparam>
    /// <typeparam name="TArg4">The type of the fourth argument.</typeparam>
    /// <typeparam name="TArg5">The type of the fifth argument.</typeparam>
    /// <typeparam name="TArg6">The type of the sixth argument.</typeparam>
    /// <typeparam name="TArg7">The type of the seventh argument.</typeparam>
    /// <typeparam name="TArg8">The type of the eighth argument.</typeparam>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="arg3">The third argument.</param>
    /// <param name="arg4">The fourth argument.</param>
    /// <param name="arg5">The fifth argument.</param>
    /// <param name="arg6">The sixth argument.</param>
    /// <param name="arg7">The seventh argument.</param>
    /// <param name="arg8">The eighth argument.</param>
    public delegate void HlAction<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8>( TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8 )
        where TArg1 : allows ref struct
        where TArg2 : allows ref struct
        where TArg3 : allows ref struct
        where TArg4 : allows ref struct
        where TArg5 : allows ref struct
        where TArg6 : allows ref struct
        where TArg7 : allows ref struct
        where TArg8 : allows ref struct
        ;
    /// <summary>
    /// Represents a Hashlink function that takes nine arguments and returns no value.
    /// </summary>
    /// <typeparam name="TArg1">The type of the first argument.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument.</typeparam>
    /// <typeparam name="TArg3">The type of the third argument.</typeparam>
    /// <typeparam name="TArg4">The type of the fourth argument.</typeparam>
    /// <typeparam name="TArg5">The type of the fifth argument.</typeparam>
    /// <typeparam name="TArg6">The type of the sixth argument.</typeparam>
    /// <typeparam name="TArg7">The type of the seventh argument.</typeparam>
    /// <typeparam name="TArg8">The type of the eighth argument.</typeparam>
    /// <typeparam name="TArg9">The type of the ninth argument.</typeparam>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="arg3">The third argument.</param>
    /// <param name="arg4">The fourth argument.</param>
    /// <param name="arg5">The fifth argument.</param>
    /// <param name="arg6">The sixth argument.</param>
    /// <param name="arg7">The seventh argument.</param>
    /// <param name="arg8">The eighth argument.</param>
    /// <param name="arg9">The ninth argument.</param>
    public delegate void HlAction<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9>( TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9 )
        where TArg1 : allows ref struct
        where TArg2 : allows ref struct
        where TArg3 : allows ref struct
        where TArg4 : allows ref struct
        where TArg5 : allows ref struct
        where TArg6 : allows ref struct
        where TArg7 : allows ref struct
        where TArg8 : allows ref struct
        where TArg9 : allows ref struct
        ;
    /// <summary>
    /// Represents a Hashlink function that takes ten arguments and returns no value.
    /// </summary>
    /// <typeparam name="TArg1">The type of the first argument.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument.</typeparam>
    /// <typeparam name="TArg3">The type of the third argument.</typeparam>
    /// <typeparam name="TArg4">The type of the fourth argument.</typeparam>
    /// <typeparam name="TArg5">The type of the fifth argument.</typeparam>
    /// <typeparam name="TArg6">The type of the sixth argument.</typeparam>
    /// <typeparam name="TArg7">The type of the seventh argument.</typeparam>
    /// <typeparam name="TArg8">The type of the eighth argument.</typeparam>
    /// <typeparam name="TArg9">The type of the ninth argument.</typeparam>
    /// <typeparam name="TArg10">The type of the tenth argument.</typeparam>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="arg3">The third argument.</param>
    /// <param name="arg4">The fourth argument.</param>
    /// <param name="arg5">The fifth argument.</param>
    /// <param name="arg6">The sixth argument.</param>
    /// <param name="arg7">The seventh argument.</param>
    /// <param name="arg8">The eighth argument.</param>
    /// <param name="arg9">The ninth argument.</param>
    /// <param name="arg10">The tenth argument.</param>
    public delegate void HlAction<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10>( TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10 )
        where TArg1 : allows ref struct
        where TArg2 : allows ref struct
        where TArg3 : allows ref struct
        where TArg4 : allows ref struct
        where TArg5 : allows ref struct
        where TArg6 : allows ref struct
        where TArg7 : allows ref struct
        where TArg8 : allows ref struct
        where TArg9 : allows ref struct
        where TArg10 : allows ref struct
        ;
}
