
using Hashlink.Proxy;
using Hashlink.Proxy.Objects;
using Hashlink.UnsafeUtilities;
using HashlinkNET.Compiler.Data;
using HaxeProxy.Runtime;
using HaxeProxy.Runtime.Internals;
using HaxeProxy.Runtime.Internals.Cache;
using Mono.Cecil;
using Newtonsoft.Json;
using System.Dynamic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace HashlinkNET.Compiler.Steps.Preprocessor.Imports
{
    internal class ImportRuntimeTypesStep : CompileStep
    {
        public const int FUNC_MAX_ARGS_COUNT = 11;
        public override void Execute( IDataContainer container )
        {
            var rdata = container.AddGlobalData<RuntimeImports>();
            var gdata = container.GetGlobalData<GlobalData>();
            var module = gdata.Module;

            var rtAsm = typeof(HaxeProxyBase).Assembly;

            rdata.runtimeName = new AssemblyNameReference(rtAsm.GetName().Name, rtAsm.GetName().Version);
            rdata.typeSystem = module.TypeSystem;
            rdata.arrowFuncCtxType = ImportType<HaxeArrowFunctionContext>();
            rdata.objBaseType = ImportType<HaxeProxyBase>();
            rdata.functionInfoCache = ImportType<FunctionInfoCache>();
            rdata.objFieldInfoCache = ImportType<ObjFieldInfoCache>();

            rdata.objectType = module.TypeSystem.Object;
            rdata.nullType = module.ImportReference(typeof(Nullable<>));
            rdata.valueTypeType = ImportType<ValueType>();
            rdata.delegateType = ImportType<Delegate>();
            rdata.delegateBaseType = ImportType<MulticastDelegate>();
            rdata.stringType = ImportType<string>();
            rdata.AsyncCallbackType = ImportType<AsyncCallback>();
            rdata.IAsyncResultType = ImportType<IAsyncResult>();
            rdata.enumBaseType = ImportType<System.Enum>();
            rdata.typeType = ImportType<Type>();

            rdata.bytesType = ImportType<nint>();
            rdata.refType = module.ImportReference(typeof(Ref<>));

            rdata.nativeArray = ImportType<HashlinkArray>();

            rdata.enumType = module.ImportReference(typeof(HaxeEnum<,>));
            rdata.virtualType = ImportType<HaxeVirtual>();
            rdata.objectBaseType = ImportType<HaxeObject>();
            rdata.dynType = ImportType<ExpandoObject>();



            rdata.objBaseCtorMethod = module.ImportReference(typeof(HaxeProxyBase)
                .GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, [
                typeof(HashlinkObj)
                ]));
            rdata.delegateDynInvokeMethod = ImportMethod(typeof(Delegate), nameof(Delegate.DynamicInvoke));
            rdata.objectGetTypeMethod = ImportMethod(typeof(object), nameof(GetType));
            rdata.typeGetFromHandleMethod = ImportMethod(typeof(Type), nameof(Type.GetTypeFromHandle));

            rdata.delegateInfoType = ImportType<DelegateInfo>();
            rdata.delegateInfoSelfField = module.ImportReference(DelegateInfo.FI_self);
            rdata.delegateInfoTargetField = module.ImportReference(DelegateInfo.FI_invokePtr);

            rdata.phGetNativeMethod = ImportPseudocodeHelperMethod(nameof(PseudocodeHelper.GetNativeMethod));
            rdata.phDynGetMethod = ImportPseudocodeHelperMethod(nameof(PseudocodeHelper.DynGet));
            rdata.phDynSetMethod = ImportPseudocodeHelperMethod(nameof(PseudocodeHelper.DynSet));
            rdata.phCreateClosure = ImportPseudocodeHelperMethod(nameof(PseudocodeHelper.CreateClosure));
            rdata.phSetGlobal = ImportPseudocodeHelperMethod(nameof(PseudocodeHelper.SetGlobal));

            rdata.phWrapException = ImportPseudocodeHelperMethod(nameof(PseudocodeHelper.WrapException));
            rdata.phUnwrapException = ImportPseudocodeHelperMethod(nameof(PseudocodeHelper.UnwrapException));
            rdata.phGetTypePointer = ImportPseudocodeHelperMethod(nameof(PseudocodeHelper.GetTypePointer));
            rdata.phGetRuntimeTypePointer = ImportPseudocodeHelperMethod(nameof(PseudocodeHelper.GetRuntimeTypePointer));
            rdata.phGetTypeId = ImportPseudocodeHelperMethod(nameof(PseudocodeHelper.GetTypeId));
            rdata.phGetRefPointer = ImportPseudocodeHelperMethod(nameof(PseudocodeHelper.GetRefPointer));
            rdata.phAsRef = ImportPseudocodeHelperMethod(nameof(PseudocodeHelper.AsRef));
            rdata.phUnref = ImportPseudocodeHelperMethod(nameof(PseudocodeHelper.Unref));
            rdata.phMakeRef = ImportPseudocodeHelperMethod(nameof(PseudocodeHelper.MakeRef));
            rdata.phMakeNullable = ImportPseudocodeHelperMethod(nameof(PseudocodeHelper.MakeNullable));
            rdata.phNullNullable = ImportPseudocodeHelperMethod(nameof(PseudocodeHelper.NullNullable));
            rdata.phGetNullableValue = ImportPseudocodeHelperMethod(nameof(PseudocodeHelper.GetNullableValue));
            rdata.phAsHaxeString = ImportPseudocodeHelperMethod(nameof(PseudocodeHelper.AsHaxeString));
            rdata.phAsHaxeStringPtr = ImportPseudocodeHelperMethod(nameof(PseudocodeHelper.AsHaxeStringPtr));

            rdata.arrayGetItem = module.ImportReference(
                typeof(HashlinkArray).GetProperty("Item")!.GetMethod);
            rdata.arraySetItem = module.ImportReference(
                typeof(HashlinkArray).GetProperty("Item")!.SetMethod);
            rdata.arrayGetCount = module.ImportReference(
                typeof(HashlinkArray).GetProperty("Count")!.GetMethod);

            rdata.refFrom = module.ImportReference(
                typeof(Ref<>).GetMethod(nameof(Ref<int>.From)));
            rdata.refValueGetter = module.ImportReference(
                typeof(Ref<>).GetProperty(nameof(Ref<int>.Value))!.GetMethod);
            rdata.refValueSetter = module.ImportReference(
                typeof(Ref<>).GetProperty(nameof(Ref<int>.Value))!.SetMethod);
            rdata.refReferenceGetter = module.ImportReference(
                typeof(Ref<>).GetProperty(nameof(Ref<int>.Reference))!.GetMethod);

            rdata.unsafeRead = module.ImportReference(
                typeof(Unsafe).GetMethods().First(m =>
                    m.Name == nameof(Unsafe.Read) &&
                    m.IsGenericMethodDefinition &&
                    m.GetParameters().Length == 1 &&
                    m.GetParameters()[0].ParameterType == typeof(void*)));
            rdata.unsafeWrite = module.ImportReference(
                typeof(Unsafe).GetMethods().First(m =>
                    m.Name == nameof(Unsafe.Write) &&
                    m.IsGenericMethodDefinition &&
                    m.GetParameters().Length == 2 &&
                    m.GetParameters()[0].ParameterType == typeof(void*)));
            rdata.unsafeAsRef = module.ImportReference(
                typeof(Unsafe).GetMethods().First(m =>
                    m.Name == nameof(Unsafe.AsRef) &&
                    m.IsGenericMethodDefinition &&
                    m.GetParameters().Length == 1 &&
                    m.GetParameters()[0].ParameterType == typeof(void*)));
            rdata.unsafeIsNullRef = module.ImportReference(
                typeof(Unsafe).GetMethods().First(m =>
                    m.Name == nameof(Unsafe.IsNullRef) &&
                    m.IsGenericMethodDefinition &&
                    m.GetParameters().Length == 1 &&
                    m.GetParameters()[0].ParameterType.IsByRef));
            rdata.unsafeAreSame = module.ImportReference(
                typeof(Unsafe).GetMethods().First(m =>
                    m.Name == nameof(Unsafe.AreSame) &&
                    m.IsGenericMethodDefinition &&
                    m.GetParameters().Length == 2 &&
                    m.GetParameters()[0].ParameterType.IsByRef));

            rdata.hCreateInstance = ImportHelperMethod(nameof(HaxeProxyHelper.CreateInstance));
            rdata.hGetCallInfoById = ImportHelperMethod(nameof(HaxeProxyHelper.GetCallInfoById));
            rdata.hGetFieldById = ImportHelperMethod(nameof(HaxeProxyHelper.GetFieldById));
            rdata.hGetValueFieldById = ImportHelperMethod(nameof(HaxeProxyHelper.GetValueFieldById));
            rdata.hSetFieldById = ImportHelperMethod(nameof(HaxeProxyHelper.SetFieldById));
            rdata.hSetValueFieldById = ImportHelperMethod(nameof(HaxeProxyHelper.SetValueFieldById));
            rdata.hGetGlobal = ImportHelperMethod(nameof(HaxeProxyHelper.GetGlobal));
            rdata.hGetProxy = ImportHelperMethod(nameof(HaxeProxyHelper.GetProxy));
            rdata.hCreateEnumInstance = ImportHelperMethod(nameof(HaxeProxyHelper.CreateEnumInstance));
            rdata.hGetEnumIndex = ImportMethod(typeof(HaxeEnum), "get_RawIndex");
            rdata.hEnumEquality = ImportMethod(typeof(HaxeEnum), "op_Equality");
            rdata.hEnumInequality = ImportMethod(typeof(HaxeEnum), "op_Inequality");
            rdata.objectEquals = module.ImportReference(typeof(object).GetMethod(
                nameof(object.Equals), [typeof(object), typeof(object)]));
            rdata.hGetTypeIndexFromType = ImportHelperMethod(nameof(HaxeProxyHelper.GetTypeIndexFromType));
            rdata.hToVirtual = module.ImportReference(
                typeof(HaxeProxyBase).GetMethod(nameof(HaxeProxyBase.ToVirtual)));
            rdata.hToObject = ImportHelperMethod(nameof(HaxeProxyHelper.ToObject));
            rdata.hGetNativeCall = ImportHelperMethod(nameof(HaxeProxyHelper.GetNativeCall));
            rdata.hGetRef = ImportHelperMethod(nameof(HaxeProxyHelper.GetRef));

            rdata.hAddHook = ImportHelperMethod(nameof(HaxeProxyHelper.AddHook));
            rdata.hRemoveHook = ImportHelperMethod(nameof(HaxeProxyHelper.RemoveHook));

            TypeReference ImportType<T>()
            {
                return module.ImportReference(typeof(T));
            }
            MethodReference ImportAttribute<T>( int argsCount = -1 )
            {
                return module.ImportReference(typeof(T).GetConstructors().First(x =>
                    argsCount < 0 || x.GetParameters().Length == argsCount));
            }

            MethodReference ImportMethod( Type type, string name )
            {
                return module.ImportReference(type.GetMethod(name));
            }

            MethodReference ImportHelperMethod( string name )
            {
                return module.ImportReference(typeof(HaxeProxyHelper).GetMethod(name));
            }

            MethodReference ImportPseudocodeHelperMethod( string name )
            {
                return module.ImportReference(typeof(PseudocodeHelper).GetMethod(name));
            }

            rdata.attrTypeBindingCtor = ImportAttribute<HaxeProxyBindingAttribute>();
            rdata.attrFIndexCtor = ImportAttribute<HashlinkFIndexAttribute>();
            rdata.attrTIndexCtor = ImportAttribute<HashlinkTIndexAttribute>();
            rdata.attrDynamic = ImportAttribute<DynamicAttribute>();
            rdata.attrDynamic2 = ImportAttribute<DynamicAttribute>(1);
            rdata.attrObsolete = ImportAttribute<ObsoleteAttribute>(1);
            rdata.attrIgnoreAccessCheck = ImportAttribute<IgnoresAccessChecksToAttribute>();
            rdata.attrInitialValue = ImportAttribute<HashlinkInitialvalueAttribute>();
            rdata.attrAlt = ImportAttribute<HashlinkAltAttribute>();
            rdata.jsonIgnoreCtor = ImportAttribute<JsonIgnoreAttribute>();

            rdata.funcTypes = new TypeReference[FUNC_MAX_ARGS_COUNT];
            for (var i = 0; i < FUNC_MAX_ARGS_COUNT; i++)
            {
                rdata.funcTypes[i] = module.ImportReference(
                    rtAsm.GetType("HaxeProxy.Runtime.HlFunc`" + (i + 1), true)
                    );
            }

            rdata.actionTypes = new TypeReference[FUNC_MAX_ARGS_COUNT];
            rdata.actionTypes[0] = module.ImportReference(typeof(HlAction));
            for (var i = 1; i < FUNC_MAX_ARGS_COUNT; i++)
            {
                rdata.actionTypes[i] = module.ImportReference(
                    rtAsm.GetType("HaxeProxy.Runtime.HlAction`" + i, true)
                    );
            }

            gdata.Assembly.CustomAttributes.Add(new(rdata.attrIgnoreAccessCheck)
            {
                ConstructorArguments =
                {
                    new(gdata.Assembly.MainModule.TypeSystem.String, typeof(HaxeProxyHelper).Assembly.GetName().Name)
                }
            });


        }
    }
}
