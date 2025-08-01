using Hashlink.Reflection.Types;
using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Data.Interfaces;
using Microsoft.VisualBasic.FileIO;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using CallSite = Mono.Cecil.CallSite;

namespace HashlinkNET.Compiler.Utils
{
    internal static class GeneralUtils
    {
        public static T ArrayLastItem<T>( this T[] array )
        {
            return array[^1];
        }
        public static unsafe string FirstCharUpper( this string str )
        {
            if (!char.IsLetter(str[0]) || char.IsUpper(str[0]))
            {
                return str;
            }
            Span<char> buff = stackalloc char[str.Length + 1];
            str.AsSpan().CopyTo(buff);
            buff[0] = char.ToUpperInvariant(buff[0]);
            return new(buff);
        }
        public static string GetEnumItemName( this HlEnumConstruct ec )
        {
            return string.IsNullOrEmpty(ec.Name) ? "Default" : ec.Name;
        }

        [return: NotNullIfNotNull(nameof(type))]
        public static TypeReference? GetTypeRef( this IDataContainer data, HlType? type )
        {
            if (type == null)
            {
                return null;
            }
            return data.TryGetData<ITypeReferenceValue>(type, out var trv) ? trv.TypeRef : data.GetData<TypeReference>(type);
        }
        public static FieldDefinition? FindField( this TypeDefinition type, string name )
        {
            return type.Fields.FirstOrDefault(x => x.Name == name);
        }

        public static MethodDefinition? FindMethod( this TypeDefinition type, string name )
        {
            return type.Methods.FirstOrDefault(x => x.Name == name);
        }

        public static void FixPIndex( this MethodDefinition method )
        {

        }

        private static FieldDefinition GetHlFieldInfoCache( TypeDefinition type, string name, RuntimeImports rdata)
        {
            var cache = type.FindField("cachedFieldInfo_" + name);
            if (cache == null)
            {
                cache = new("cachedFieldInfo_" + name, FieldAttributes.Private | FieldAttributes.Static,
                    rdata.objFieldInfoCache);
                type.Fields.Add(cache);
            }
            return cache;
        }
        public static void EmitFieldGetterSetter( this TypeDefinition type, 
            PropertyDefinition property,
            IDataContainer container,
            string name)
        {
            var gdata = container.GetGlobalData<GlobalData>();
            {
                property.SetMethod = new("set_" + name, MethodAttributes.SpecialName | MethodAttributes.Public,
                    gdata.Module.TypeSystem.Void)
                {
                    Parameters =
                        {
                            new(property.PropertyType)
                        }
                };
                var ilp = property.SetMethod.Body.GetILProcessor();
                ilp.Emit(OpCodes.Ldarg_0);
                ilp.Emit(OpCodes.Ldarg_1);
                type.EmitSetHlField(ilp, container, name, property.PropertyType);
                ilp.Emit(OpCodes.Ret);
            }
            {
                property.GetMethod = new("get_" + name, MethodAttributes.SpecialName | MethodAttributes.Public,
                    property.PropertyType);
                property.GetMethod.MethodReturnType.CheckDynamic(
                    container.GetGlobalData<RuntimeImports>(), property.PropertyType);
                var ilp = property.GetMethod.Body.GetILProcessor();
                ilp.Emit(OpCodes.Ldarg_0);
                type.EmitGetHlField(ilp, container, name, property.PropertyType);
                ilp.Emit(OpCodes.Ret);
            }
            type.Methods.Add(property.SetMethod);
            type.Methods.Add(property.GetMethod);

        }
        public static void EmitGetHlField( this TypeDefinition type, ILProcessor il,
            IDataContainer container,
            string name, TypeReference ft)
        {
            var rdata = container.GetGlobalData<RuntimeImports>();
            var cache = GetHlFieldInfoCache(type, name, rdata);
            if (ft is GenericParameter || 
                ft.Namespace == "System" && ft.Name.StartsWith("Nullable^1") && ft.IsValueType)
            {
                il.Emit(OpCodes.Ldstr, name);
                il.Emit(OpCodes.Ldsflda, cache);
                il.Emit(OpCodes.Call, rdata.hGetFieldById.MakeInstance(ft));
                il.Emit(OpCodes.Unbox_Any, ft);
            }
            else if (ft.IsValueType)
            {
                il.Emit(OpCodes.Ldstr, name);
                il.Emit(OpCodes.Ldsflda, cache);
                il.Emit(OpCodes.Call, rdata.hGetValueFieldById.MakeInstance(ft));
            }
            else
            {
                il.Emit(OpCodes.Ldstr, name);
                il.Emit(OpCodes.Ldsflda, cache);
                il.Emit(OpCodes.Call, rdata.hGetFieldById.MakeInstance(ft));
            }
        }
        public static void EmitSetHlField( this TypeDefinition type, ILProcessor il,
            IDataContainer container,
            string name, TypeReference ft)
        {
            var rdata = container.GetGlobalData<RuntimeImports>();
            var cache = GetHlFieldInfoCache(type, name, rdata);

            if (ft is GenericParameter ||
                ft.Namespace == "System" && ft.Name.StartsWith("Nullable^1") && ft.IsValueType
                )
            {
                il.Emit(OpCodes.Box, ft);
                il.Emit(OpCodes.Ldstr, name);
                il.Emit(OpCodes.Ldsflda, cache);
                il.Emit(OpCodes.Call, rdata.hSetFieldById);
            }
            else if (ft.IsValueType)
            {
                il.Emit(OpCodes.Ldstr, name);
                il.Emit(OpCodes.Ldsflda, cache);
                il.Emit(OpCodes.Call, rdata.hSetValueFieldById.MakeInstance(ft));
            }
            else
            {
                il.Emit(OpCodes.Ldstr, name);
                il.Emit(OpCodes.Ldsflda, cache);
                il.Emit(OpCodes.Call, rdata.hSetFieldById);
            }
        }

        public static void EmitCallHlFunc( this TypeDefinition type, ILProcessor il, 
            IDataContainer container,
            HlFunction func, Action<ILProcessor, int> emitArg )
        {
            var rdata = container.GetGlobalData<RuntimeImports>();
            var ft = ((HlTypeWithFun)func.Type.Value).FunctionDescription;
            var cache = type.FindField("cachedFuncInfo_" + func.FunctionIndex);
            if (cache == null)
            {
                cache = new("cachedFuncInfo_" + func.FunctionIndex, FieldAttributes.Private | FieldAttributes.Static,
                    rdata.functionInfoCache);
                type.Fields.Add(cache);
            }
            var di = il.Body.Variables.FirstOrDefault(x => x.VariableType == rdata.delegateInfoType);
            if (di == null)
            {
                di = new(rdata.delegateInfoType);
                il.Body.Variables.Add(di);
            }

            il.Emit(OpCodes.Ldc_I4, func.FunctionIndex);
            il.Emit(OpCodes.Ldsflda, cache);
            il.Emit(OpCodes.Call, rdata.hGetCallInfoById);
            il.Emit(OpCodes.Stloc, di);

            il.Emit(OpCodes.Ldloc, di);
            il.Emit(OpCodes.Ldfld, rdata.delegateInfoSelfField);

            var cs = new CallSite(container.GetTypeRef(ft.ReturnType.Value))
            {
                HasThis = true
            };

            for (var i = 0; i < ft.Arguments.Length; i++)
            {
                var at = ft.Arguments[i].Value;
                var pt = container.GetTypeRef(at);

                
                emitArg(il, i);
                var pd = new ParameterDefinition(pt);
                if (at.Kind == HlTypeKind.Null)
                {
                    il.Emit(OpCodes.Box, pt);
                    pd.ParameterType = rdata.objectType;
                }
                //pd.CheckDynamic(rdata, pd.ParameterType);
                cs.Parameters.Add(pd);
            }

            il.Emit(OpCodes.Ldloc, di);
            il.Emit(OpCodes.Ldfld, rdata.delegateInfoTargetField);
            il.Emit(OpCodes.Calli, cs);

            if (ft.ReturnType.Value.Kind != HlTypeKind.Void)
            {
                if (!cs.ReturnType.IsValueType)
                {
                    il.Emit(OpCodes.Call, new GenericInstanceMethod(rdata.hGetProxy)
                    {
                        GenericArguments = {cs.ReturnType}
                    });
                }
                else if(ft.ReturnType.Value.Kind == HlTypeKind.Null)
                {
                    il.Emit(OpCodes.Unbox, cs.ReturnType);
                    cs.ReturnType = rdata.objectType;
                }
            }
        }

        public static MethodReference MakeInstance( this MethodReference method,
            params ReadOnlySpan<TypeReference> types )
        {
            if (types.Length == 0)
            {
                return method;
            }
            var gm = new GenericInstanceMethod(method);
            foreach (var v in types)
            {
                gm.GenericArguments.Add(v);
            }
            return gm;
        }

        public static void AddDynamicAttribute( this ICustomAttributeProvider provider, RuntimeImports runtimeImports )
        {
            provider.CustomAttributes.Add(new(runtimeImports.attrDynamic));
        }
        public static void CheckDynamic( this ICustomAttributeProvider provider, RuntimeImports runtimeImports,
            TypeReference type )
        {
            if (type.Namespace == "System" && type.Name == "Object")
            {
                provider.AddDynamicAttribute(runtimeImports);
            }
        }

        public static bool ParseHlTypeName( string name, out string @namespace, out string typeName )
        {
            if (string.IsNullOrEmpty(name))
            {
                @namespace = "dc";
                typeName = "";
                return false;
            }
            var lastDot = name.LastIndexOf('.');
            if (lastDot == -1)
            {
                @namespace = "dc";
                typeName = name;
            }
            else
            {
                @namespace = "dc." + name[..lastDot];
                typeName = name[(lastDot + 1)..];
            }
            if (typeName.StartsWith('$'))
            {
                typeName = "_" + typeName[1..];
            }
            return true;
        }
    }
}
