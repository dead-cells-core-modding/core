using Hashlink;
using ModCore.Modules;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ModCore.Hashlink
{
    public static unsafe class HashlinkUtils
    {
        private readonly static Dictionary<nint, string> stringsLookup = [];
        private readonly static Dictionary<string, nint> name2hltype = [];

        private readonly static Dictionary<nint, nint> funcNativePtr = [];
        private readonly static Dictionary<string, Dictionary<string, nint>> name2func = [];
        private readonly static Dictionary<string, int> hltype2globalIdx = [];

        private readonly static Dictionary<string, HashlinkObject> hltype2globalObject = [];
        
        public static IReadOnlyDictionary<string, nint> GetHashlinkTypes()
        {
            return name2hltype;
        }


        internal static void Initialize(HL_code* code, HL_module* m)
        {
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

                if (f->obj == null || f->field == null)
                {
                    continue;
                }
                var tname = GetString(f->obj->name);
                if (!name2func.TryGetValue(tname, out var funcTable))
                {
                    funcTable = [];
                    name2func.Add(tname, funcTable);
                }


                var name = GetString(f->field);

                funcTable[name] = (nint)f;
                funcNativePtr[(nint)f] = (nint)fp;
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
        }

        public static void* GetGlobalData(string name)
        {
            if(!hltype2globalIdx.TryGetValue(name, out var idx))
            {
                return null;
            }
            HL_module* module = HashlinkVM.Instance.Context->m;
            return (void*)(module->globals_data + module->globals_indexes[idx]);
        }

        public static HL_vdynamic* CreateDynamic(HL_type* type, void* ptr)
        {
            var vdyn = HashlinkNative.hl_alloc_dynamic(type);
            vdyn->val.ptr = ptr;
            return vdyn;
        }

        public static HashlinkObject GetGlobal(HL_type* type)
        {
            var name = GetString(type->data.obj->name);
            var gPtr = GetGlobalData(name);
            if (!hltype2globalObject.TryGetValue(name, out var val))
            {
                var dyn = CreateDynamic(type, gPtr);
                hltype2globalObject[name] = val = HashlinkObject.FromHashlink(dyn);
            }
            return val;
        }

        public static object GetData(void* ptr, HL_type* type)
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
                HL_type.TypeKind.HDYN => HashlinkObject.FromHashlink((HL_vdynamic*)ptr),
                _ => HashlinkObject.FromHashlink(CreateDynamic(type, ptr))
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
        

        public static HL_function* FindFunction(HL_type* type, string name)
        {
            var tname = GetString(type->data.obj->name);
            if (!name2func.TryGetValue(tname, out var table) ||
                !table.TryGetValue(name, out var result))
            {
                Log.Logger.Information("AA: {a} {b} {c}", table, name, tname);
                return null;
            }
            return (HL_function*)result;
        }
        public static void* GetFunctionNativePtr(HL_function* func)
        {
            return (void*) funcNativePtr[(nint)func];
        }

        

        public static int HLHash(string str)
        {
            int h = 0;
            fixed (char* pname = str)
            {
                char* name = pname;
                while (*name != 0)
                {
                    h = 223 * h + (int) name;
                    name++;
                }
            }
            return h;
        }
    }
}
