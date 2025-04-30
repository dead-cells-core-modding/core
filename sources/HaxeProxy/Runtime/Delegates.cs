using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaxeProxy.Runtime
{
    public delegate TRet HlFunc<TRet>() 
        where TRet : allows ref struct
        ;
    public delegate TRet HlFunc<TRet, TArg1>(TArg1 arg1)
        where TRet : allows ref struct
        where TArg1 : allows ref struct
        ;
    public delegate TRet HlFunc<TRet, TArg1, TArg2>( TArg1 arg1 , TArg2 arg2 )
        where TRet : allows ref struct
        where TArg1 : allows ref struct
        where TArg2 : allows ref struct
        ;
    public delegate TRet HlFunc<TRet, TArg1, TArg2, TArg3>( TArg1 arg1, TArg2 arg2, TArg3 arg3 )
        where TRet : allows ref struct
        where TArg1 : allows ref struct
        where TArg2 : allows ref struct
        where TArg3 : allows ref struct
        ;
    public delegate TRet HlFunc<TRet, TArg1, TArg2, TArg3, TArg4>( TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4 )
        where TRet : allows ref struct
        where TArg1 : allows ref struct
        where TArg2 : allows ref struct
        where TArg3 : allows ref struct
        where TArg4 : allows ref struct
        ;
    public delegate TRet HlFunc<TRet, TArg1, TArg2, TArg3, TArg4, TArg5>( TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5 )
        where TRet : allows ref struct
        where TArg1 : allows ref struct
        where TArg2 : allows ref struct
        where TArg3 : allows ref struct
        where TArg4 : allows ref struct
        where TArg5 : allows ref struct
        ;
    public delegate TRet HlFunc<TRet, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>( TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6 )
        where TRet : allows ref struct
        where TArg1 : allows ref struct
        where TArg2 : allows ref struct
        where TArg3 : allows ref struct
        where TArg4 : allows ref struct
        where TArg5 : allows ref struct
        where TArg6 : allows ref struct
        ;
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

    public delegate void HlAction();
    public delegate void HlAction<TArg1>( TArg1 arg1 )
        where TArg1 : allows ref struct
        ;
    public delegate void HlAction<TArg1, TArg2>( TArg1 arg1, TArg2 arg2 )
        where TArg1 : allows ref struct
        where TArg2 : allows ref struct
        ;
    public delegate void HlAction<TArg1, TArg2, TArg3>( TArg1 arg1, TArg2 arg2, TArg3 arg3 )
        where TArg1 : allows ref struct
        where TArg2 : allows ref struct
        where TArg3 : allows ref struct
        ;
    public delegate void HlAction<TArg1, TArg2, TArg3, TArg4>( TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4 )
        where TArg1 : allows ref struct
        where TArg2 : allows ref struct
        where TArg3 : allows ref struct
        where TArg4 : allows ref struct
        ;
    public delegate void HlAction<TArg1, TArg2, TArg3, TArg4, TArg5>( TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5 )
        where TArg1 : allows ref struct
        where TArg2 : allows ref struct
        where TArg3 : allows ref struct
        where TArg4 : allows ref struct
        where TArg5 : allows ref struct
        ;
    public delegate void HlAction<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>( TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6 )
        where TArg1 : allows ref struct
        where TArg2 : allows ref struct
        where TArg3 : allows ref struct
        where TArg4 : allows ref struct
        where TArg5 : allows ref struct
        where TArg6 : allows ref struct
        ;
    public delegate void HlAction<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>( TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7 )
        where TArg1 : allows ref struct
        where TArg2 : allows ref struct
        where TArg3 : allows ref struct
        where TArg4 : allows ref struct
        where TArg5 : allows ref struct
        where TArg6 : allows ref struct
        where TArg7 : allows ref struct
        ;
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
