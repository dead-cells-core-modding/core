using Hashlink;
using ModCore.Hashlink;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Utils;
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
            [TK.HARRAY] = typeof(HashlinkObject[]),
            [TK.HOBJ] = typeof(HashlinkObject),

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
            public readonly Dictionary<string, FieldItem> fields = [];

            public class FieldItem
            {
                public PropertyDefinition property = null!;
                public string name = "";
            }
        }
        
        private readonly List<string> debugFileNames = [];
        private readonly Dictionary<nint, FuncItem> functions = [];
        private readonly Dictionary<nint, ObjTypeItem> objTypes = [];
        private readonly Dictionary<nint, TypeReference> missingTypes = [];

       
        private void AddMetadataAttribute(ICustomAttributeProvider provider, string name, HashlinkMetadataRef.HLMType type,
            long data = 0)
        {
            provider.CustomAttributes.Add(new(mr_hashlinkMetadataRef)
            {
                ConstructorArguments =
                        {
                            new(module.TypeSystem.String, name),
                            new(module.ImportReference(typeof(HashlinkMetadataRef.HLMType)), type),
                            new(module.ImportReference(typeof(long)), data)
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
                objName = HashlinkUtils.GetString(type->name),
            };

            if(type->super != null)
            {
                item.parent = LoadObjType(type->super->data.obj);
            }

            int lastDot = item.objName.LastIndexOf('.');
            item.type = new(lastDot == -1 ? "" : item.objName[..lastDot],
                lastDot == -1 ? item.objName : item.objName[(lastDot + 1)..],
                TypeAttributes.Public);
            if(item.isStatic = item.type.Name.StartsWith('$'))
            {
                item.type.Name = item.type.Name.Replace('$', '_');
            }
            AddMetadataAttribute(item.type, item.objName, HashlinkMetadataRef.HLMType.ObjType,
                item.isStatic ? 1 : 2
            );
            

            if(item.parent != null)
            {
                item.type.BaseType = item.parent.type;
            }
            else
            {
                item.type.BaseType = module.ImportReference(typeof(HashlinkObject));
            }

            //Fields
            for(int i = 0; i < type->nfields; i++)
            {
                var f = type->fields + i;
                var name = HashlinkUtils.GetString(f->name);
                var ft = GetTypeRef(f->t);

                var setter = new MethodDefinition("set_" + name, MethodAttributes.Public, module.TypeSystem.Void)
                {
                    IsSetter = true,
                    Parameters =
                    {
                        new(ft)
                    }
                };
                AddMetadataAttribute(setter, name, HashlinkMetadataRef.HLMType.Field);


                var getter = new MethodDefinition("get_" + name, MethodAttributes.Public, ft)
                {
                    IsGetter = true,
                };
                AddMetadataAttribute(getter, name, HashlinkMetadataRef.HLMType.Field);

                var prop = new PropertyDefinition("p_" + name, PropertyAttributes.None, ft)
                {
                    GetMethod = getter,
                    SetMethod = setter,
                    Parameters =
                    {
                        new(ft)
                    }
                };
                if(item.isStatic)
                {
                    setter.IsStatic  = true;
                    getter.IsStatic = true;
                }
                item.type.Methods.Add(setter);
                item.type.Methods.Add(getter);
                item.type.Properties.Add(prop);

                item.fields[name] = new()
                {
                    name = name,
                    property = prop,
                };
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
                logger.Verbose("Type: {t:x}->{ot}", (nint)type->data.obj, objType.type);
                return objType.type;
            }
            if (allowMissing)
            {
                if (!missingTypes.TryGetValue((nint)type, out var missingType))
                {
                    missingType = new TypeReference("", "missingType", module, selfRef);
                    missingTypes[(nint)type] = missingType;
                    logger.Verbose("Missing type: {type:x}", (nint)type);
                }
                return missingType;
            }

            return module.ImportReference(typeof(HashlinkObject));
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
                funcName = HashlinkUtils.GetString(f->field),
                obj = f->obj,
                objType = LoadObjType(f->obj),
                startline = f->debug[1],
                filename = HashlinkUtils.GetString(
                    code->debugfiles[f->debug[0]],
                    code->debugfiles_lens[f->debug[0]],
                    Encoding.UTF8
                    ),
                tfunc = f->type->data.func
            };
            func.objName = func.objType.objName;

            func.method = new(func.funcName, MethodAttributes.Public | MethodAttributes.HideBySig |
                MethodAttributes.SpecialName, module.TypeSystem.Void);

            if (func.objType.isStatic)
            {
                func.method.IsStatic = true;
                func.method.HasThis = false;
            }
            else
            {
                func.method.IsVirtual = true;
            }

            func.method.Body = new(func.method);
            AddMetadataAttribute(func.method, func.funcName, HashlinkMetadataRef.HLMType.Function);

            

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

            logger.Verbose("Fixed missing type: {src}->{dst}", (nint)type, tr);
        }
        private void FixupObjectCtor(ObjTypeItem obj)
        {
            if(obj.isStatic)
            {
                return;
            }
            var stNsp = obj.type.Namespace;
            var stName = "_" + obj.type.Name;
            var stType = module.Types.FirstOrDefault(x => x.Namespace == stNsp && x.Name == stName);
            if(stType == null)
            {
                return;
            }
            var stCtor = stType.FindMethod("__constructor__");
            if(stCtor == null)
            {
                return;
            }
            var ctor = new MethodDefinition(".ctor", MethodAttributes.Public | 
                MethodAttributes.SpecialName |
                MethodAttributes.RTSpecialName, module.TypeSystem.Void);
            obj.type.Methods.Add(ctor);
            ctor.Body = new(ctor);
            var il = ctor.Body.GetILProcessor();
            il.Emit(OpCodes.Ldarg_0);
            for(int i = 1; i < stCtor.Parameters.Count; i++)
            {
                var p = stCtor.Parameters[i];
                var cp = new ParameterDefinition(p.Name, p.Attributes, p.ParameterType);
                ctor.Parameters.Add(cp);
                il.Emit(OpCodes.Ldarg, cp);
            }
            il.Emit(OpCodes.Call, stCtor);
            il.Emit(OpCodes.Ret);

        }
        public void Emit()
        {

            foreach(var t in HashlinkUtils.GetHashlinkTypes())
            {
                var g = (HL_type*) t.Value;
                logger.Verbose("Type: {type}", g->kind);
                if (g->kind == TK.HOBJ)
                {
                    LoadObjType(g->data.obj);
                }
            }

            logger.Information("Collecting functions");
            for (int i = 0; i < code->nfunctions; i++)
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
            foreach((nint _, var obj) in objTypes)
            {
                FixupObjectCtor(obj);
            }
        }
    }
}
