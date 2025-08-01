
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
            if (!gdata.Config.GeneratePseudocode)
            {
                rdata.nativeArray = ImportType<HashlinkArray>();
            }
            else
            {
                rdata.nativeArray = ImportType<Array>();
            }
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

            rdata.phToVirtual = ImportPseudocodeHelperMethod(nameof(PseudocodeHelper.ToVirtual));
            rdata.phGetNativeMethod = ImportPseudocodeHelperMethod(nameof(PseudocodeHelper.GetNativeMethod));
            rdata.phDynGetMethod = ImportPseudocodeHelperMethod(nameof(PseudocodeHelper.DynGet));
            rdata.phDynSetMethod = ImportPseudocodeHelperMethod(nameof(PseudocodeHelper.DynSet));
            rdata.phCreateObject = ImportPseudocodeHelperMethod(nameof(PseudocodeHelper.CreateObject));
            rdata.phCreateClosure = ImportPseudocodeHelperMethod(nameof(PseudocodeHelper.CreateClosure));
            rdata.phReadMem = ImportPseudocodeHelperMethod(nameof(PseudocodeHelper.ReadMem));
            rdata.phWriteMem = ImportPseudocodeHelperMethod(nameof(PseudocodeHelper.WriteMem));

            rdata.hCreateInstance = ImportHelperMethod(nameof(HaxeProxyHelper.CreateInstance));
            rdata.hGetCallInfoById = ImportHelperMethod(nameof(HaxeProxyHelper.GetCallInfoById));
            rdata.hGetFieldById = ImportHelperMethod(nameof(HaxeProxyHelper.GetFieldById));
            rdata.hGetValueFieldById = ImportHelperMethod(nameof(HaxeProxyHelper.GetValueFieldById));
            rdata.hSetFieldById = ImportHelperMethod(nameof(HaxeProxyHelper.SetFieldById));
            rdata.hSetValueFieldById = ImportHelperMethod(nameof(HaxeProxyHelper.SetValueFieldById));
            rdata.hGetGlobal = ImportHelperMethod(nameof(HaxeProxyHelper.GetGlobal));
            rdata.hGetProxy = ImportHelperMethod(nameof(HaxeProxyHelper.GetProxy));
            rdata.hGetNullableProxy = ImportHelperMethod(nameof(HaxeProxyHelper.GetNullableProxy));
            rdata.hCreateEnumInstance = ImportHelperMethod(nameof(HaxeProxyHelper.CreateEnumInstance));
            rdata.hGetEnumIndex = ImportMethod(typeof(HaxeEnum), "get_RawIndex");

            rdata.hAddHook = ImportHelperMethod(nameof(HaxeProxyHelper.AddHook));
            rdata.hRemoveHook = ImportHelperMethod(nameof(HaxeProxyHelper.RemoveHook));

            TypeReference ImportType<T>()
            {
                return module.ImportReference(typeof(T));
            }
            MethodReference ImportAttribute<T>()
            {
                return module.ImportReference(typeof(T).GetConstructors().First());
            }

            MethodReference ImportMethod(Type type, string name)
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
        }
    }
}
