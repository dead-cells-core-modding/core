using HashlinkNET.Bytecode;
using HashlinkNET.Compiler.Data;
using HashlinkNET.Compiler.Data.Interfaces;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
        public static TypeReference? GetAltTypeRef( this IDataContainer data, HlType? type )
        {
            if (type == null)
            {
                return null;
            }
            return data.TryGetData<IAltTypeReferenceValue>(type, out var trv) ? trv.AltTypeReference : null;
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

        private static FieldReference GetHlFieldInfoCache( TypeDefinition type, string name, RuntimeImports rdata )
        {
            var cache = type.FindField("cachedFieldInfo_" + name);
            if (cache == null)
            {
                cache = new("cachedFieldInfo_" + name, FieldAttributes.Private | FieldAttributes.Static,
                    rdata.objFieldInfoCache);
                type.Fields.Add(cache);
            }
            if (type.GenericParameters.Count != 0)
            {
                return new FieldReference(cache.Name, cache.FieldType,
                    type.MakeGenericInstanceType([.. type.GenericParameters]));
            }
            return cache;
        }
        public static void EmitFieldGetterSetter( this TypeDefinition type,
            PropertyDefinition property,
            IDataContainer container,
            string name )
        {
            property.CheckDynamic(
                    container.GetGlobalData<RuntimeImports>(), property.PropertyType);
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
                property.SetMethod.Parameters[0].CheckDynamic(
                    container.GetGlobalData<RuntimeImports>(), property.PropertyType);

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
            string name, TypeReference ft )
        {
            var rdata = container.GetGlobalData<RuntimeImports>();
            var cache = GetHlFieldInfoCache(type, name, rdata);
            if (ft.Namespace == "System" && ft.Name.StartsWith("Nullable", StringComparison.Ordinal) && ft.IsValueType)
            {
                il.Emit(OpCodes.Ldstr, name);
                il.Emit(OpCodes.Ldsflda, cache);
                il.Emit(OpCodes.Call, rdata.hGetFieldById.MakeInstance(rdata.objectType));
                il.Emit(OpCodes.Unbox_Any, ft);
            }
            else if (ft.IsValueType)
            {
                il.Emit(OpCodes.Ldstr, name);
                il.Emit(OpCodes.Ldsflda, cache);
                il.Emit(OpCodes.Call, rdata.hGetValueFieldById.MakeInstance(ft));
            }
            else if (ft.IsGenericParameter)
            {
                il.Emit(OpCodes.Ldstr, name);
                il.Emit(OpCodes.Ldsflda, cache);
                il.Emit(OpCodes.Call, rdata.hGetFieldById.MakeInstance(rdata.objectType));
                il.Emit(OpCodes.Call, rdata.hGetProxy.MakeInstance(ft));
                il.Emit(OpCodes.Unbox_Any, ft);
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
            string name, TypeReference ft )
        {
            var rdata = container.GetGlobalData<RuntimeImports>();
            var cache = GetHlFieldInfoCache(type, name, rdata);

            if (ft is GenericParameter ||
                (ft.Namespace == "System" && ft.Name.StartsWith("Nullable", StringComparison.Ordinal) && ft.IsValueType)
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
                il.Emit(OpCodes.Castclass, ft);
                il.Emit(OpCodes.Ldstr, name);
                il.Emit(OpCodes.Ldsflda, cache);
                il.Emit(OpCodes.Call, rdata.hSetFieldById);
            }
        }

        public static void EmitCallHlFunc( this TypeDefinition type, ILProcessor il,
            IDataContainer container,
            HlFunction func, Action<ILProcessor, int> emitArg)
        {
            lock (type)
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

                var passed = Instruction.Create(OpCodes.Nop);

                il.Emit(OpCodes.Ldc_I4, func.FunctionIndex);
                il.Emit(OpCodes.Ldsflda, cache);
                il.Emit(OpCodes.Call, rdata.hGetCallInfoById);
                il.Emit(OpCodes.Stloc, di);

                il.Emit(OpCodes.Ldloc, di);
                il.Emit(OpCodes.Brfalse, passed);

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
                            GenericArguments = { cs.ReturnType }
                        });
                    }
                    else if (ft.ReturnType.Value.Kind == HlTypeKind.Null)
                    {
                        il.Emit(OpCodes.Unbox_Any, cs.ReturnType);
                        cs.ReturnType = rdata.objectType;
                    }
                }

                il.Emit(OpCodes.Ret);

                il.Append(passed);
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
                Debug.Assert(!v.IsByReference);
                gm.GenericArguments.Add(v);
            }
            return gm;
        }

        /// <summary>
        /// Builds a <c>Ref&lt;T&gt;.From(ref T)</c> reference for a concrete
        /// element type. The type parameter belongs to the generic declaring
        /// type, so the declaring type itself must be instantiated.
        /// </summary>
        public static MethodReference MakeRefFrom( this RuntimeImports rdata, TypeReference element )
        {
            var refT = rdata.refType.MakeGenericInstanceType(element);
            var result = new MethodReference("From", refT, refT)
            {
                HasThis = false
            };
            result.Parameters.Add(new(new ByReferenceType(element)));
            return result;
        }

        /// <summary>
        /// Builds the <c>Ref&lt;T&gt;.value</c> field reference (a byref field)
        /// for a concrete element type.
        /// </summary>
        public static FieldReference MakeRefValueField( this RuntimeImports rdata, TypeReference element )
        {
            var refT = rdata.refType.MakeGenericInstanceType(element);
            return new FieldReference("value", new ByReferenceType(element), refT);
        }

        public static bool IsNullable( this TypeReference type, [NotNullWhen(true)] out TypeReference? underlying )
        {
            if (type is not GenericInstanceType nullable ||
               nullable.ElementType.FullName != "System.Nullable`1" ||
               nullable.GenericArguments.Count != 1)
            {
                underlying = null;
                return false;
            }
            underlying = nullable.GenericArguments[0];
            return true;
        }

        /// <summary>
        /// If the emitted value is a <c>Nullable&lt;T&gt;</c>, replace it on the
        /// stack with its underlying value (default for null) and return the
        /// underlying type.
        /// </summary>
        public static TypeReference? EmitNullableUnwrap( this ILProcessor il,
            IDataContainer container, TypeReference? type )
        {
            if (type is GenericInstanceType nullable &&
                nullable.ElementType.FullName == "System.Nullable`1" &&
                nullable.GenericArguments.Count == 1)
            {
                var underlying = nullable.GenericArguments[0];
                il.Emit(OpCodes.Box, type);
                il.Emit(OpCodes.Unbox_Any, underlying);
                return underlying;
            }
            return type;
        }

        /// <summary>
        /// Emits a conditional branch. If the branch leaves the current
        /// protected region (try/handler), a conditional skip plus
        /// <c>leave</c> is emitted, because brtrue/brfalse may not cross a
        /// region boundary.
        /// </summary>
        public static void EmitProtectedBranch( this ILProcessor il,
            OpCode branch, Instruction target )
        {
            var body = il.Body;
            if (body.Instructions.Count == 0)
            {
                il.Emit(branch, target);
                return;
            }

            var current = body.Instructions[^1];
            var currentIndex = body.Instructions.IndexOf(current);
            var targetIndex = body.Instructions.IndexOf(target);
            var needLeave = false;

            foreach (var eh in body.ExceptionHandlers)
            {
                var tryStart = body.Instructions.IndexOf(eh.TryStart);
                var tryEnd = eh.TryEnd == null
                    ? body.Instructions.Count
                    : body.Instructions.IndexOf(eh.TryEnd);
                var handlerStart = eh.HandlerStart == null
                    ? -1
                    : body.Instructions.IndexOf(eh.HandlerStart);
                var handlerEnd = eh.HandlerEnd == null
                    ? body.Instructions.Count
                    : body.Instructions.IndexOf(eh.HandlerEnd);

                var inTry = currentIndex >= tryStart && currentIndex < tryEnd;
                var inHandler = handlerStart >= 0 &&
                    currentIndex >= handlerStart && currentIndex < handlerEnd;
                if (!inTry && !inHandler)
                {
                    continue;
                }

                var targetInTry = targetIndex >= tryStart && targetIndex < tryEnd;
                var targetInHandler = handlerStart >= 0 &&
                    targetIndex >= handlerStart && targetIndex < handlerEnd;
                if ((inTry && !targetInTry) || (inHandler && !targetInHandler))
                {
                    needLeave = true;
                    break;
                }
            }

            if (!needLeave)
            {
                il.Emit(branch, target);
                return;
            }

            var skip = Instruction.Create(OpCodes.Nop);
            var inverse = branch.Code switch
            {
                Code.Brtrue or Code.Brtrue_S => OpCodes.Brfalse,
                Code.Brfalse or Code.Brfalse_S => OpCodes.Brtrue,
                Code.Beq or Code.Beq_S => OpCodes.Bne_Un,
                Code.Bne_Un or Code.Bne_Un_S => OpCodes.Beq,
                Code.Bgt or Code.Bgt_S => OpCodes.Ble,
                Code.Ble or Code.Ble_S => OpCodes.Bgt,
                Code.Blt or Code.Blt_S => OpCodes.Bge,
                Code.Bge or Code.Bge_S => OpCodes.Blt,
                Code.Bgt_Un or Code.Bgt_Un_S => OpCodes.Ble_Un,
                Code.Ble_Un or Code.Ble_Un_S => OpCodes.Bgt_Un,
                Code.Blt_Un or Code.Blt_Un_S => OpCodes.Bge_Un,
                Code.Bge_Un or Code.Bge_Un_S => OpCodes.Blt_Un,
                _ => OpCodes.Brfalse
            };
            il.Emit(inverse, skip);
            il.Emit(OpCodes.Leave, target);
            il.Append(skip);
        }

        /// <summary>
        /// Builds <c>new Ref&lt;T&gt;(ref T)</c> for a concrete element type.
        /// </summary>
        public static MethodReference MakeRefCtor( this RuntimeImports rdata, TypeReference element )
        {
            var refT = rdata.refType.MakeGenericInstanceType(element);
            var ctor = new MethodReference(".ctor", rdata.typeSystem.Void, refT)
            {
                HasThis = true
            };
            ctor.Parameters.Add(new(new ByReferenceType(element)));
            return ctor;
        }

        public static MethodReference CreateGenericInstanceTypeMethod( this MethodReference method, TypeReference newType )
        {
            var result = new MethodReference(method.Name, method.ReturnType, newType)
            {
                ExplicitThis = method.ExplicitThis,
                HasThis = method.HasThis
            };
            foreach (var v in method.Parameters)
            {
                var pd = new ParameterDefinition(v.Name, v.Attributes, v.ParameterType);
                pd.CopyAttributeFrom(v);
                result.Parameters.Add(pd);
            }
            result.MethodReturnType.CopyAttributeFrom(method.MethodReturnType);

            return result;
        }

        public static void CopyAttributeFrom( this ICustomAttributeProvider self, ICustomAttributeProvider other )
        {
            foreach (var v in other.CustomAttributes)
            {
                self.CustomAttributes.Add(v);
            }
        }

        public static void AddDynamicAttribute( this ICustomAttributeProvider provider, RuntimeImports runtimeImports )
        {
            provider.CustomAttributes.Add(new(runtimeImports.attrDynamic));
        }

        public static HlType GetLocalRegType( this HlFunction func, int idx )
        {
            return func.LocalVariables[idx].Value;
        }

        private static bool IsObjectType( TypeReference type )
        {
            return type.Namespace == "System" && type.Name == "Object";
        }

        [ThreadStatic]
        private static List<bool>? dynamicFlagsList;

        public static void CheckDynamic( this ICustomAttributeProvider provider, RuntimeImports runtimeImports,
            TypeReference type )
        {
            dynamicFlagsList ??= [];

            if (type is GenericInstanceType)
            {
                bool anyDynamic = false;
                void CheckDynamic2( List<bool> values, TypeReference tr )
                {
                    if (IsObjectType(tr))
                    {
                        anyDynamic = true;
                        values.Add(true);
                        return;
                    }
                    values.Add(false);
                    if (tr is GenericInstanceType git)
                    {
                        foreach (var v in git.GenericArguments)
                        {
                            CheckDynamic2(values, v);
                        }
                    }
                }
                dynamicFlagsList.Clear();
                CheckDynamic2(dynamicFlagsList, type);

                if (anyDynamic)
                {
                    var array = dynamicFlagsList.ToArray();
                    provider.CustomAttributes.Add(new(runtimeImports.attrDynamic2)
                    {
                        ConstructorArguments = {
                            new(runtimeImports.typeSystem.Boolean.MakeArrayType(), array)
                        }
                    });
                }
            }
            else if (IsObjectType(type))
            {
                provider.AddDynamicAttribute(runtimeImports);
            }
        }
        public static void SetObsolete( this ICustomAttributeProvider provider, RuntimeImports runtimeImports,
            string msg )
        {
            provider.CustomAttributes.Add(new(runtimeImports.attrObsolete)
            {
                ConstructorArguments =
                {
                    new(runtimeImports.stringType, msg)
                }
            });
        }
        public static void SetDifferentBetweenVersion( this ICustomAttributeProvider provider, RuntimeImports runtimeImports )
        {
            provider.SetObsolete(runtimeImports, "Note that this type/method/field/property may not exist in other versions of the game.");
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
