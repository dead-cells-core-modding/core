using Hashlink;
using ModCore.Hashlink;
using Mono.Cecil;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Hashlink.HL_module;
using TK = Hashlink.HL_type.TypeKind;

namespace ModCore.Generator
{
    internal unsafe class HLAGenerator(
        AssemblyDefinition assembly, 
        ILogger logger,
        HL_code* code)
    {
        private readonly ModuleDefinition module = assembly.MainModule;
        private readonly AssemblyNameReference modCoreRef = new("ModCore", new());
        private readonly AssemblyNameReference selfRef = assembly.Name;

        private readonly MethodReference mr_hashlinkMetadataRef = assembly.MainModule
            .ImportReference(typeof(HashlinkMetadataRef).GetConstructors()[0]);

        private static readonly Dictionary<TK, Type> hltype2net = new()
        {
            [TK.HI32] = typeof(int),
            [TK.HI64] = typeof(long),
            [TK.HF32] = typeof(float),
            [TK.HF64] = typeof(double),
            [TK.HUI8] = typeof(byte),
            [TK.HUI16] = typeof(ushort),
            [TK.HBOOL] = typeof(bool),
            [TK.HVOID] = typeof(void),
            [TK.HBYTES] = typeof(byte[]),
            [TK.HARRAY] = typeof(object[]),
            [TK.HOBJ] = typeof(object),
        };
        private unsafe class FuncItem
        {
            public string filename = "";
            public int startline;

            public string objName = "";
            public string funcName = "";

            public ObjTypeItem? objType;

            public HL_function* func;
            public HL_type_obj* obj;
            public HL_type_func* tfunc;

            public MethodDefinition method = null!;
        }
        private unsafe class ObjTypeItem
        {
            public string objName = "";
            public bool isStatic;
            public HL_type_obj* obj;

            public ObjTypeItem? parent;

            public TypeDefinition type = null!;
        }
        private readonly Dictionary<nint, string> stringsLookup = [];
        private readonly List<string> debugFileNames = [];
        private readonly Dictionary<nint, FuncItem> functions = [];
        private readonly Dictionary<nint, ObjTypeItem> objTypes = [];
        private readonly Dictionary<nint, TypeReference> missingTypes = [];
        

        private string GetString(char* ch)
        {
            if(stringsLookup.TryGetValue((nint)ch, out var s))
            {
                return s;
            }
            return stringsLookup[(nint)ch] = new string(ch);
        }
        private string GetString(byte* ch, int num, Encoding encoding)
        {
            if (stringsLookup.TryGetValue((nint)ch, out var s))
            {
                return s;
            }
            return stringsLookup[(nint)ch] = encoding.GetString(ch, num);
        }
        private void AddMetadataAttribute(ICustomAttributeProvider provider, string name, HashlinkMetadataRef.HLMType type)
        {
            provider.CustomAttributes.Add(new(mr_hashlinkMetadataRef)
            {
                ConstructorArguments =
                        {
                            new(module.TypeSystem.String, name),
                            new(module.ImportReference(typeof(HashlinkMetadataRef.HLMType)), type)
                        }
            });
        }
        private ObjTypeItem LoadObjType(HL_type_obj* type)
        {
            if(objTypes.TryGetValue((nint)type, out var result))
            {
                return result;
            }
            var item = new ObjTypeItem()
            {
                obj = type,
                objName = GetString(type->name),
            };

            if(type->super != null)
            {
                item.parent = LoadObjType(type->super->data.obj);
            }

            int lastDot = item.objName.LastIndexOf('.');
            item.type = new(lastDot == -1 ? "" : item.objName[..lastDot],
                lastDot == -1 ? item.objName : item.objName[(lastDot + 1)..],
                TypeAttributes.Public);
            AddMetadataAttribute(item.type, item.objName, HashlinkMetadataRef.HLMType.ObjType);
            item.isStatic = item.type.Name.StartsWith('$');

            if(item.parent != null)
            {
                item.type.BaseType = item.parent.type;
            }
            else
            {
                item.type.BaseType = module.TypeSystem.Object;
            }

            //Fields
            for(int i = 0; i < type->nfields; i++)
            {
                var f = type->fields + i;
                var fd = new FieldDefinition(GetString(f->name), FieldAttributes.Public, GetTypeRef(f->t));
                if(item.isStatic)
                {
                    fd.IsStatic  = true;
                }
                item.type.Fields.Add(fd);
            }

            module.Types.Add(item.type);

            objTypes[(nint)type] = item;
            return item;
        }
        private TypeReference GetTypeRef(HL_type* type, bool allowMissing = true)
        {
            if (hltype2net.TryGetValue(type->kind, out var result))
            {
                return module.ImportReference(result);
            }
            if(objTypes.TryGetValue((nint)type->data.obj, out var objType))
            {
                logger.Information("Type: {t:x}->{ot}", (nint)type->data.obj, objType.type);
                return objType.type;
            }
            if (allowMissing)
            {
                if (!missingTypes.TryGetValue((nint)type, out var missingType))
                {
                    missingType = new TypeReference("", "missingType", module, selfRef);
                    missingTypes[(nint)type] = missingType;
                    logger.Information("Missing type: {type:x}", (nint)type);
                }
                return missingType;
            }

            return module.TypeSystem.Object;
        }
        private FuncItem LoadFunc(HL_function* f)
        {
            if (functions.TryGetValue((nint)f, out var func))
            {
                return func;
            }

            func = new FuncItem()
            {
                func = f,
                funcName = GetString(f->field),
                obj = f->obj,
                objType = LoadObjType(f->obj),
                startline = f->debug[1],
                filename = GetString(
                    code->debugfiles[f->debug[0]],
                    code->debugfiles_lens[f->debug[0]],
                    Encoding.UTF8
                    ),
                tfunc = f->type->data.func
            };
            func.objName = func.objType.objName;

            func.method = new(func.funcName, MethodAttributes.Public | MethodAttributes.HideBySig, module.TypeSystem.Void);
            AddMetadataAttribute(func.method, func.funcName, HashlinkMetadataRef.HLMType.Function);

            if(func.objType.isStatic)
            {
                func.method.IsStatic = true;
            }
            else
            {
                func.method.IsVirtual = true;
            }

            for(int i = 0; i < func.tfunc->nargs; i++)
            {
                var arg = func.tfunc->args[i];
               
                func.method.Parameters.Add(new("arg" + i, ParameterAttributes.None, GetTypeRef(arg)));
            }
            func.method.ReturnType = GetTypeRef(func.tfunc->ret);

            func.objType.type.Methods.Add(func.method);

            return functions[(nint)f] = func;
        }
        private void FixupMissingTypeRef(HL_type* type, TypeReference tr)
        {
            var t = GetTypeRef(type, false);
            tr.Namespace = t.Namespace;
            tr.Name = t.Name;
            if (t.DeclaringType != null)
            {
                tr.DeclaringType = module.ImportReference(t.DeclaringType);
            }
            if (!t.IsDefinition)
            {
                tr.Scope = t.Scope;
            }
            else
            {
                tr.Scope = selfRef;
            }

            logger.Information("Fixed missing type: {src}->{dst}", (nint)type, tr);
        }
        public void Emit()
        {
            logger.Information("Collecting functions");

            for(int i = 0; i < code->nfunctions; i++)
            {
                var f = code->functions + i;
                if(f->field == null || f->obj == null)
                {
                    continue;
                }

                var func = LoadFunc(f);
                logger.Verbose("Function: {objname}->{name}({argNum}) in {fn}", func.objName,
                    func.funcName,
                    func.tfunc->nargs,
                    func.filename
                    );
            }

            logger.Information("Found {num} functions in total", functions.Count);

            logger.Information("Fixing missing type references");
            foreach((nint ptr, var tr) in missingTypes)
            {
                FixupMissingTypeRef((HL_type*)ptr, tr);
            }
        }
    }
}
