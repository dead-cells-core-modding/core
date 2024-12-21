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

        public static void* GetGlobalData(HL_type* type)
        {
            if(type->kind != HL_type.TypeKind.HOBJ)
            {
                return null;
            }
            if(!hltype2globalIdx.TryGetValue(GetString(type->data.obj->name), out var idx))
            {
                return null;
            }
            return (void*) HashlinkVM.Instance.Context->m->globals_data[idx];
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
