using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ModCore.Native
{
    public unsafe partial class TCCCompiler : IDisposable
    {
        private const string LIBTCC = "libtcc";

        [LibraryImport(LIBTCC)]
        private static partial nint tcc_new();
        [LibraryImport(LIBTCC)]
        private static partial void tcc_delete( nint s );
        [LibraryImport(LIBTCC)]
        private static partial void tcc_set_lib_path( nint s, [MarshalAs(UnmanagedType.LPUTF8Str)] string path );

        public delegate void TCCErrorFunc( void* opaque, byte* msg);
        [LibraryImport(LIBTCC)]
        private static partial void tcc_set_error_func( nint s, void* error_opaque, TCCErrorFunc error_func );
        [LibraryImport(LIBTCC)]
        private static partial int tcc_set_options( nint s, [MarshalAs(UnmanagedType.LPUTF8Str)] string str );
        [LibraryImport(LIBTCC)]
        private static partial int tcc_add_file(nint s, [MarshalAs(UnmanagedType.LPUTF8Str)] string str );
        [LibraryImport(LIBTCC)]
        private static partial int tcc_add_include_path( nint s, [MarshalAs(UnmanagedType.LPUTF8Str)] string str );
        [LibraryImport(LIBTCC)]
        private static partial int tcc_add_sysinclude_path( nint s, [MarshalAs(UnmanagedType.LPUTF8Str)] string str );
        [LibraryImport(LIBTCC)]
        private static partial int tcc_compile_string(nint s, [MarshalAs(UnmanagedType.LPUTF8Str)] string str );
        [LibraryImport(LIBTCC)]
        private static partial int tcc_add_library_path(nint s, [MarshalAs(UnmanagedType.LPUTF8Str)] string str );
        [LibraryImport(LIBTCC)]
        private static partial int tcc_set_output_type( nint s, int output_tyoe );
        [LibraryImport(LIBTCC)]
        private static partial int tcc_output_file(nint s, [MarshalAs(UnmanagedType.LPUTF8Str)] string filepath );
        [LibraryImport(LIBTCC)]
        private static partial int tcc_add_symbol( nint s, [MarshalAs(UnmanagedType.LPUTF8Str)] string name, nint ptr );
        [LibraryImport(LIBTCC)]
        private static partial nint tcc_get_symbol( nint s, [MarshalAs(UnmanagedType.LPUTF8Str)] string name );
        [LibraryImport(LIBTCC)]
        private static partial int tcc_relocate( nint s );

        private nint tcc_state;
        private bool disposedValue;
        private TCCErrorFunc error_handler;

        public TCCCompiler()
        {
            tcc_state = tcc_new();
            error_handler = TCCErrorHandler;

            tcc_set_error_func(tcc_state, null, error_handler);
        }

        public event Action<string?>? OnError;

        public enum OutputType
        {
            MEMORY = 1,
            EXE = 2,
            DLL = 4,
            OBJ = 3,
            PREPROCESS = 5
        }

        public int SetOutputType( OutputType type )
        {
            ObjectDisposedException.ThrowIf(disposedValue, this);

            return tcc_set_output_type(tcc_state, (int)type);
        }

        public int AddString( string str )
        {
            ObjectDisposedException.ThrowIf(disposedValue, this);


            return tcc_compile_string(tcc_state, str);
        }

        public int AddFile( string path )
        {
            ObjectDisposedException.ThrowIf(disposedValue, this);

            return tcc_add_file(tcc_state, GetPathPtr(path));
        }

        public int Link( string outputPath )
        {
            ObjectDisposedException.ThrowIf(disposedValue, this);

            return tcc_output_file(tcc_state, GetPathPtr(outputPath));
        }

        public int AddOptions( string options )
        {
            ObjectDisposedException.ThrowIf(disposedValue, this);

            return tcc_set_options(tcc_state, options);
        }

        public int AddLibraryPath( string path )
        {
            ObjectDisposedException.ThrowIf(disposedValue, this);

            return tcc_add_library_path(tcc_state, GetPathPtr(path));
        }

        public int AddIncludePath( string path, bool isSystem )
        {
            ObjectDisposedException.ThrowIf(disposedValue, this);

            if (isSystem)
            {
                return tcc_add_sysinclude_path(tcc_state, GetPathPtr(path));
            }
            else
            {
                return tcc_add_include_path(tcc_state, GetPathPtr(path));
            }
        }

        public int AddSymbol( string name, nint ptr )
        {
            ObjectDisposedException.ThrowIf(disposedValue, this);

            return tcc_add_symbol(tcc_state, name, ptr);
        }

        public nint GetSymbol( string name )
        {
            ObjectDisposedException.ThrowIf(disposedValue, this);

            return tcc_get_symbol(tcc_state, name);
        }

        public int Relocate()
        {
            ObjectDisposedException.ThrowIf(disposedValue, this);

            return tcc_relocate(tcc_state);
        }

        private string GetPathPtr( string path )
        {
            return path.Replace('\\', '/');
        }

        private void TCCErrorHandler( void* opaque, byte* msg )
        {
            OnError?.Invoke(Marshal.PtrToStringUTF8((nint)msg));
        }

        protected virtual void Dispose( bool disposing )
        {
            if (!disposedValue)
            {
                tcc_delete(tcc_state);
                tcc_state = 0;

                disposedValue = true;
            }
        }

        ~TCCCompiler()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
