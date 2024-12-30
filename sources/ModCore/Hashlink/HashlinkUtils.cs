using Hashlink;
using ModCore.Modules;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ModCore.Hashlink
{
    public static unsafe class HashlinkUtils
    {
        private readonly static Dictionary<nint, string> stringsLookup = [];
        private readonly static Dictionary<string, nint> name2hltype = [];

        private readonly static Dictionary<nint, nint> hlfun2hlfunction = [];
        private readonly static Dictionary<nint, nint> funcNativePtr = [];
        private readonly static Dictionary<int, nint> fid2hlfuctionn = [];
        private readonly static Dictionary<string, int> hltype2globalIdx = [];

        private readonly static Dictionary<string, HashlinkObject> hltype2globalObject = [];

        public static HL_type* HLType_String { get; private set; }
        
        public static IReadOnlyDictionary<string, nint> GetHashlinkTypes()
        {
            return name2hltype;
        }

        public static nint HLJit_Start { get; internal set; }
        public static nint HLJit_End { get; internal set; }
        public static HL_module* Module { get; internal set; }

        internal static void Initialize(HL_code* code, HL_module* m)
        {
            Module = m;
            HLJit_Start = (nint) m->jit_code;
            HLJit_End = HLJit_Start + m->codesize;
            for (int i = 0; i < code->ntypes; i++)
            {
                var g = code->types + i;
                string name;


                if (g->data.obj == null)
                {
                    continue;
                }

                if (g->kind == HL_type.TypeKind.HOBJ)
                {
                    var n = g->data.obj->name;
                    name = GetString(n);

                }
                else if (g->kind == HL_type.TypeKind.HENUM)
                {
                    name = GetString(g->data.tenum->name);
                }
                else
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }
                name2hltype[name] = (nint)g;
            }
            for (int i = 0; i < code->nfunctions; i++)
            {
                var f = code->functions + i;
                var fp = m->functions_ptrs[f->findex];

                fid2hlfuctionn[f->findex] = (nint) f;
                hlfun2hlfunction[(nint)f->type->data.func] = (nint)f;
                funcNativePtr[(nint)f] = (nint)fp;
                
                if (f->obj == null)
                {
                    if(f->field.field == null)
                    {
                        continue;
                    }

                    continue;
                }
            }
            for (int i = 0; i < code->nglobals; i++)
            {
                var g = code->globals[i];
                if(g->kind != HL_type.TypeKind.HOBJ)
                {
                    continue;
                }
                var name = GetString(g->data.obj->name);
                hltype2globalIdx[name] = i;
            }

            HLType_String = FindTypeFromName("String");
        }

        public static HL_function* GetFunction(HL_type_func* type)
        {
            return (HL_function*) hlfun2hlfunction[(nint)type];
        }
        public static string GetFunctionName(HL_function* func)
        {
            if (func->obj != null)
            {
                return $"{GetString(func->obj->name)}.{GetString(func->field.field)}@{func->findex}";
            }
            else if (func->field.@ref != null)
            {
                return GetFunctionName(func->field.@ref);
            }
            else
            {
               return $"fun${func->findex}";
            }
        }

        public static string GetTypeString(HL_type* type)
        {
            return Marshal.PtrToStringUni((nint)hl_type_str(type))!;
        }

        public static bool HasThis(HL_function* func)
        {
            return func->obj != null && !IsGlobal(func->obj) && (
            func->type->data.func->nargs > 0 && func->type->data.func->args[0]->data.obj == func->obj);
        }

        public static bool IsGlobal(HL_type_obj* obj)
        {
            var c = obj->name;
            while(*c != 0)
            {
                if (c[0] == '$')
                {
                    return true;
                }
                c++;
            }
            return false;
        }
        public static HL_vdynamic* GetGlobalData(string name)
        {
            if(!hltype2globalIdx.TryGetValue(name, out var idx))
            {
                return null;
            }
            HL_module* module = HashlinkVM.Instance.Context->m;
            return *(HL_vdynamic**)(module->globals_data + module->globals_indexes[idx]);
        }

        public static HL_vdynamic* CreateDynamic(HL_type* type, void* ptr)
        {
            var vdyn = hl_alloc_dynamic(type);
            vdyn->val.ptr = ptr;
            return vdyn;
        }

        public static HashlinkObject GetGlobal(string name)
        {
            var gPtr = GetGlobalData(name);
            if (!hltype2globalObject.TryGetValue(name, out var val))
            {
                hltype2globalObject[name] = val = HashlinkObject.FromHashlink(gPtr);
            }
            return val;
        }

        public static bool IsValidHLObject(void* ptr)
        {
            if(!Native.mcn_memory_readable(ptr))
            {
                return false;
            }
            HL_type* ptype = (HL_type*) *(void**)ptr;
            if (!Native.mcn_memory_readable(ptype))
            {
                return false;
            }
            if(ptype->kind < 0 || ptype->kind >= HL_type.TypeKind.HLAST)
            {
                return false;
            }
            return true;
        }

        public static void SetData(void* ptr, HL_type* type, object? val)
        {
            if(val == null)
            {
                *(nint*)ptr = 0;
                return;
            }
            if (val is HashlinkObject obj)
            {
                if (type->kind == HL_type.TypeKind.HDYN || !obj.IsDynamic)
                {
                    *(nint*)ptr = (nint)obj.AsDynamic;
                }
                else if (type->kind.IsPointer())
                {
                    *(nint*)ptr = (nint)obj.AsDynamic->val.ptr;
                }
                else if (type->kind == HL_type.TypeKind.HF64 || type->kind == HL_type.TypeKind.HI64)
                {
                    *(long*)ptr = obj.AsDynamic->val.i64;
                }
                else
                {
                    *(int*)ptr = obj.AsDynamic->val.@int;
                }
                return;
            }
            else if(val is string str)
            {
                if(type->kind == HL_type.TypeKind.HBYTES)
                {
                    *(nint*)ptr = (nint)GetHLBytesString(str)->val.ptr;
                }
                else
                {
                    SetData(ptr, type, GetHLString(str));
                }
                return;
            }
            if (type->kind == HL_type.TypeKind.HUI8)
            {
                *(byte*)ptr = (byte)val;
            }
            else if(type->kind == HL_type.TypeKind.HUI16)
            {
                *(ushort*)ptr = (ushort)val;
            }
            else if(type->kind == HL_type.TypeKind.HI32)
            {
                *(int*)ptr = (int)val;
            }
            else if(type->kind == HL_type.TypeKind.HI64)
            {
                *(long*)ptr = (long)val;
            }
            else if(type->kind == HL_type.TypeKind.HF32)
            {
                *(float*)ptr = (float)val;
            }
            else if(type->kind == HL_type.TypeKind.HF64)
            {
                if(val is float v)
                {
                    val = (double)v;
                }
                *(double*)ptr = (double)val;
            }
            else if(type->kind == HL_type.TypeKind.HBOOL)
            {
                *(bool*)ptr = (bool)val;
            }
            else if(type->kind == HL_type.TypeKind.HVOID)
            {
                return;
            }
            else
            {
                *(nint*)ptr = (nint)val;
            }
        }
        public static bool IsPointer(this HL_type.TypeKind kind)
        {
            return kind >= HL_type.TypeKind.HBYTES;
        }
        public static object? GetData(void* ptr, HL_type* type)
        {
            return type->kind switch
            {
                HL_type.TypeKind.HUI8 => *(byte*)ptr,
                HL_type.TypeKind.HUI16 => *(ushort*)ptr,
                HL_type.TypeKind.HI32 => *(int*)ptr,
                HL_type.TypeKind.HI64 => *(long*)ptr,
                HL_type.TypeKind.HF32 => *(float*)ptr,
                HL_type.TypeKind.HF64 => *(double*)ptr,
                HL_type.TypeKind.HBOOL => *(bool*)ptr,
                HL_type.TypeKind.HBYTES => *(nint*)ptr,
                HL_type.TypeKind.HVOID => null,
                HL_type.TypeKind.HABSTRACT => *(nint*)ptr,
                _ => IsValidHLObject(*(void**)ptr) ? 
                    HashlinkObject.FromHashlink(*(HL_vdynamic**)ptr) :
                    *(nint*)ptr,
            };
        }

        public static string GetString(char* ch)
        {
            //It is too bad, but I dont know how to do
            if ((nint)ch < 0x1ffff)
            {
                return "";
            }
            if (stringsLookup.TryGetValue((nint)ch, out var s))
            {
                return s;
            }
            return stringsLookup[(nint)ch] = new string(ch);
        }
        public static string GetString(byte* ch, int num, Encoding encoding)
        {
            if (stringsLookup.TryGetValue((nint)ch, out var s))
            {
                return s;
            }
            return stringsLookup[(nint)ch] = encoding.GetString(ch, num);
        }
        public static HL_type* FindTypeFromName(string name)
        {
            return (HL_type*) name2hltype[name];
        }
        
        public static HL_function* GetFunction(string type, string name)
        {
            return GetFunction(FindTypeFromName(type), name);
        }
        public static HL_function* GetFunction(HL_type* type, string name)
        {
           
            var tname = GetString(type->data.obj->name);

            if(type->kind != HL_type.TypeKind.HOBJ)
            {
                return null;
            }
            var hashed = HLHash(name);
            var obj = type->data.obj;
            for (int i = 0; i < obj->nproto; i++)
            {
                var p = obj->proto + i;
                if(p->hashed_name == hashed)
                {
                    return (HL_function*) fid2hlfuctionn[p->findex];
                }
            }
            //Find super
            if(obj->super != null)
            {
                return GetFunction(obj->super, name);
            }
            return null;
        }
        public static void* GetFunctionNativePtr(HL_function* func)
        {
            return (void*) funcNativePtr[(nint)func];
        }

        public static void* HLAlloc(HL_type* type, int size, HL_Alloc_Flags flags)
        {
            return hl_gc_alloc_gen(type, size, flags);
        }
        public static void* HLAllocNoPtr(int size)
        {
            return HLAlloc(hlt_bytes, size, HL_Alloc_Flags.MEM_KIND_NOPTR);
        }
        public static void* HLAllocRaw(int size)
        {
            return HLAlloc(hlt_abstract, size, HL_Alloc_Flags.MEM_KIND_RAW);
        }

        public static HashlinkObject GetHLString(string str)
        {
            fixed (char* c = str)
            {
                var hstr = HashlinkObject.FromHashlink(GetHLBytesString(str));
                var vobj = new HashlinkObject(HLType_String).Dynamic;
                vobj.length = str.Length;
                vobj.bytes = (nint)hstr.ValuePointer;
                
                return vobj;
            }
        }

        public static HL_vdynamic* GetHLBytesString(string str)
        {
            var dyn = hl_alloc_dynamic(hlt_bytes);
            var strlen = str.Length * 2 + 2;
            var bytes = (char*)HLAllocNoPtr(strlen);
            
            fixed (char* src = str)
            {
                Buffer.MemoryCopy(src, bytes, strlen, strlen);
            }
            
            dyn->val.ptr = bytes;
            return dyn;
        }

        public static int HLHash(string str)
        {
            fixed (char* pname = str)
            {
                return hl_hash_gen(pname, false);
            }
            
        }
    }
}
