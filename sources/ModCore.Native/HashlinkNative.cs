﻿
using ModCore;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

#pragma warning disable CA1401

namespace Hashlink
{
    public static unsafe partial class HashlinkNative
    {
        private const string LIBHL = "libhl";

        public static class InternalTypes
        {
            private static readonly nint hLibhl = NativeLibrary.Load(LIBHL);

            private static readonly Dictionary<Type, nint> net2hltype = [];
            private static readonly Dictionary<TypeKind, Type> hltype2net = [];

            public static readonly HL_type* hlt_void = GetType(typeof(void));
            public static readonly HL_type* hlt_i32 = GetType(typeof(int));
            public static readonly HL_type* hlt_i64 = GetType(typeof(long));
            public static readonly HL_type* hlt_f64 = GetType(typeof(double));
            public static readonly HL_type* hlt_f32 = GetType(typeof(float));
            public static readonly HL_type* hlt_dyn = GetType(null);
            public static readonly HL_type* hlt_array = GetType(null);
            public static readonly HL_type* hlt_bytes = GetType(typeof(byte*));
            public static readonly HL_type* hlt_dynobj = GetType(null);
            public static readonly HL_type* hlt_bool = GetType(typeof(bool));
            public static readonly HL_type* hlt_abstract = GetType(null);
            private static HL_type* GetType( Type? type, [CallerMemberName] string name = "" )
            {
                var ptr = (HL_type*)NativeLibrary.GetExport(hLibhl, name);
                if (type != null)
                {
                    net2hltype[type] = (nint)ptr;
                    hltype2net[ptr->kind] = type;
                }
                return ptr;
            }

            public static Type? GetFrom( TypeKind type )
            {
                return hltype2net.TryGetValue(type, out var result) ? result : null;
            }
            public static HL_type* GetFrom( Type type )
            {
                return net2hltype.TryGetValue(type, out var result) ? (HL_type*)result : null;
            }

            static InternalTypes()
            {
                NativeLibrary.Free(hLibhl);
                hLibhl = 0;
            }
        }
        [LibraryImport(LIBHL)]
        public static partial void hl_global_init();
        [LibraryImport(LIBHL)]
        public static partial HL_thread_info* hl_get_thread();

        [LibraryImport(LIBHL)]
        public static partial HL_array* hl_alloc_array( HL_type* at, int size );

        [LibraryImport(LIBHL)]
        public static partial HL_vdynamic* hl_dyn_call_safe( HL_vclosure* c, HL_vdynamic** args, int nargs, bool* isException );

        [LibraryImport(LIBHL)]
        public static partial char* hl_to_string( HL_vdynamic* v );
        [LibraryImport(LIBHL)]
        [DoesNotReturn]
        public static partial void hl_throw( HL_vdynamic* v );
        [LibraryImport(LIBHL)]
        [DoesNotReturn]
        public static partial void hl_rethrow( HL_vdynamic* v );

        [LibraryImport(LIBHL)]
        public static partial HL_vvirtual* hl_to_virtual( HL_type* vt, HL_vdynamic* obj );

        [LibraryImport(LIBHL)]
        public static partial HL_vdynobj* hl_alloc_dynobj();
        [LibraryImport(LIBHL)]
        public static partial HL_vdynamic* hl_alloc_dynamic( HL_type* t );
        [LibraryImport(LIBHL)]
        public static partial HL_vdynamic* hl_alloc_obj( HL_type* type );
        [LibraryImport(LIBHL)]
        public static partial HL_enum* hl_alloc_enum( HL_type* type , int index );
        [LibraryImport(LIBHL)]
        public static partial HL_field_lookup* hl_lookup_find( HL_field_lookup* l, int size, int hash );

        [LibraryImport(LIBHL)]
        public static partial long hl_dyn_geti64( HL_vdynamic* d, int hfield, HL_type* t );
        [LibraryImport(LIBHL)]
        public static partial void hl_dyn_seti64( HL_vdynamic* d, int hfield, HL_type* t, long val );

        [LibraryImport(LIBHL)]
        public static partial int hl_dyn_geti( HL_vdynamic* d, int hfield, HL_type* t );
        [LibraryImport(LIBHL)]
        public static partial void hl_dyn_seti( HL_vdynamic* d, int hfield, HL_type* t, int val );

        [LibraryImport(LIBHL)]
        public static partial void* hl_dyn_getp( HL_vdynamic* d, int hfield, HL_type* t );
        [LibraryImport(LIBHL)]
        public static partial void hl_dyn_setp( HL_vdynamic* d, int hfield, HL_type* t, void* val );

        [LibraryImport(LIBHL)]
        public static partial float hl_dyn_getf( HL_vdynamic* d, int hfield, HL_type* t );
        [LibraryImport(LIBHL)]
        public static partial void hl_dyn_setf( HL_vdynamic* d, int hfield, HL_type* t, float val );

        [LibraryImport(LIBHL)]
        public static partial double hl_dyn_getd( HL_vdynamic* d, int hfield, HL_type* t );
        [LibraryImport(LIBHL)]
        public static partial void hl_dyn_setd( HL_vdynamic* d, int hfield, HL_type* t, double val );

        [LibraryImport(LIBHL)]
        public static partial char* hl_resolve_symbol( void* addr, char* @out, ref int outSize );
        [LibraryImport(LIBHL)]
        public static partial HL_field_lookup* obj_resolve_field( HL_type_obj* o, int hfield );
        [LibraryImport(LIBHL)]
        public static partial void* hl_obj_lookup( HL_vdynamic* d, int hfield, out HL_type* t );
        [LibraryImport(LIBHL)]
        public static partial HL_vdynamic* hl_obj_lookup_extra( HL_vdynamic* d, int hfield );
        [LibraryImport(LIBHL)]
        public static partial void* hl_obj_lookup_set( HL_vdynamic* d, int hfield, HL_type* t, out HL_type* ft );
        [LibraryImport(LIBHL)]
        public static partial int hl_hash_gen( char* name, [MarshalAs(UnmanagedType.Bool)] bool cache_name );
        [LibraryImport(LIBHL)]
        public static partial HL_vdynamic* hl_make_dyn( void* data, HL_type* t );
        [LibraryImport(LIBHL)]
        public static partial HL_vdynamic* hl_obj_get_field( HL_vdynamic* obj, int hfield );
        [LibraryImport(LIBHL)]
        public static partial void hl_obj_set_field( HL_vdynamic* obj, int hfield, HL_vdynamic* v );
        [LibraryImport(LIBHL)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool hl_obj_has_field( HL_vdynamic* obj, int hfield );
        [LibraryImport(LIBHL)]
        public static partial char* hl_type_str( HL_type* t );
        [LibraryImport(LIBHL)]
        public static partial void* hl_gc_alloc_gen( HL_type* t, int size, HL_Alloc_Flags flags );
        [LibraryImport(LIBHL)]
        public static partial int hl_type_size( HL_type* t );
        [LibraryImport(LIBHL)]
        public static partial HL_vvirtual* hl_alloc_virtual( HL_type* t );
        [LibraryImport(LIBHL)]
        public static partial HL_vclosure* hl_alloc_closure_ptr( HL_type* fullt, void* fvalue, void* v );
        [LibraryImport(LIBHL)]
        public static partial HL_vclosure* hl_alloc_closure_void( HL_type* fullt, void* fvalue );
        [LibraryImport(LIBHL)]
        public static partial void* hl_code_read( void* data, int size, byte** errorMsg );
        [LibraryImport(LIBHL)]
        public static partial void hl_dump_stack();
        [LibraryImport(LIBHL)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool module_resolve_pos( HL_module* m, void* addr, out int fidx, out int fpos );
        [LibraryImport(LIBHL)]
        public static partial char* module_resolve_symbol( void* addr, char* @out, ref int outSize );
        [LibraryImport(LIBHL)]
        public static partial void hl_gc_set_flags( HL_GC_Flags flags );
        [LibraryImport(LIBHL)]
        public static partial HL_GC_Flags hl_gc_get_flags();
        [LibraryImport(LIBHL)]
        public static partial char* hl_field_name( int hashedName );
        [LibraryImport(LIBHL)]
        public static partial void* hl_alloc_executable_memory( int size );
        [LibraryImport(LIBHL)]
        [SuppressGCTransition]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool hl_is_gc_ptr( void* ptr );

        [LibraryImport(LIBHL)]
        public static partial void* hl_jit_alloc( );
        [LibraryImport(LIBHL)]
        public static partial void hl_jit_init( void* ctx, HL_module* m );
        [LibraryImport(LIBHL)]
        public static partial int hl_jit_function( void* ctx, HL_module* m, HL_function* f, long* uConstants );
        [LibraryImport(LIBHL)]
        public static partial void hl_jit_free( void* ctx, [MarshalAs(UnmanagedType.I1)] bool can_reset );
        [LibraryImport(LIBHL)]
        public static partial void* hl_jit_code( void* ctx, HL_module* m, int* codesize, void** debug, void* previous );
        [LibraryImport(LIBHL)]
        [SuppressGCTransition]
        public static partial int hl_gc_get_memsize( void* ptr ); 
        [LibraryImport(LIBHL)]
        public static partial HL_array* hl_exception_stack( );

        public delegate void NativeEventHandleDelegate( int eventId, nint data);
        [LibraryImport(LIBHL)]
        public static partial void hl_event_set_handler( NativeEventHandleDelegate handler );
        [LibraryImport(LIBHL)]
        public static partial void hl_sys_init( void** args, int nargs, void* hlfile );
        [LibraryImport(LIBHL)]
        public static partial void hl_register_thread( void* stack_top );
        [LibraryImport(LIBHL)]
        public static partial HL_module* hl_module_alloc( HL_code* c );
        [LibraryImport(LIBHL)]
        public static partial int hl_module_init( HL_module* m, int hot_reload, int vtune_later );
        [LibraryImport(LIBHL)]
        public static partial void hl_blocking( int b );
        [LibraryImport(LIBHL)]
        public static partial void hl_add_root( void* v );
        [LibraryImport(LIBHL)]
        public static partial void hl_remove_root( void* v );
        [LibraryImport(LIBHL)]
        public static partial HL_gc_threads* hl_gc_threads_info(); 
        [LibraryImport(LIBHL)]
        public static partial void gc_global_lock( [MarshalAs(UnmanagedType.I1)] bool b );
        [LibraryImport(LIBHL)]
        public static partial int gc_allocator_get_block_id( HL_gc_pheader* page, void* block );
        [LibraryImport(LIBHL)] 
        public static partial int gc_flush_mark( HL_gc_mstack* st, [MarshalAs(UnmanagedType.I1)] bool single );
        [LibraryImport(LIBHL)]
        public static partial void** hl_gc_mark_grow( HL_gc_mstack* stack );
        [LibraryImport(LIBHL)]
        public static partial HL_gc_pheader* hl_gc_get_page( void* v );
    }
}
