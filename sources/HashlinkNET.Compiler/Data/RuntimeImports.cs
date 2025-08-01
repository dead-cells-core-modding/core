using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#nullable disable

namespace HashlinkNET.Compiler.Data
{
    internal class RuntimeImports
    {
        public AssemblyNameReference runtimeName;

        public TypeReference objBaseType;
        public TypeReference nativeArray;
        public TypeReference refType;
        public TypeReference enumType;
        public TypeReference arrowFuncCtxType;
        public TypeReference virtualType;
        public TypeReference dynType;
        public TypeReference bytesType;
        public TypeReference objectType;

        public TypeReference delegateInfoType;
        public FieldReference delegateInfoSelfField;
        public FieldReference delegateInfoTargetField;

        public TypeReference functionInfoCache;
        public TypeReference objFieldInfoCache;

        public MethodReference attrTypeBindingCtor;
        public MethodReference attrFIndexCtor;
        public MethodReference attrTIndexCtor;
        public MethodReference attrDynamic;
        public MethodReference jsonIgnoreCtor;

        public MethodReference delegateDynInvokeMethod;
        public MethodReference objectGetTypeMethod;
        public MethodReference typeGetFromHandleMethod;
        public MethodReference objBaseCtorMethod;

        public MethodReference hCreateInstance;
        public MethodReference hGetFieldById;
        public MethodReference hGetValueFieldById;
        public MethodReference hSetFieldById;
        public MethodReference hSetValueFieldById;
        public MethodReference hGetGlobal;
        public MethodReference hGetProxy;
        public MethodReference hGetNullableProxy;
        public MethodReference hGetCallInfoById;
        public MethodReference hCreateEnumInstance;

        public MethodReference hAddHook;
        public MethodReference hRemoveHook;

        public TypeReference stringType;

        public TypeReference IAsyncResultType;
        public TypeReference AsyncCallbackType;

        public TypeReference nullType;
        public TypeReference valueTypeType;
        public TypeReference delegateType;
        public TypeReference delegateBaseType;
        public TypeReference enumBaseType;
        public TypeReference typeType;


        public TypeReference[] funcTypes;
        public TypeReference[] actionTypes;

        public MethodReference phToVirtual;
        public MethodReference phGetNativeMethod;
        public MethodReference phDynGetMethod;
        public MethodReference phDynSetMethod;
        public MethodReference phCreateObject;
        public MethodReference phCreateClosure;
        public TypeReference objectBaseType;
        public MethodReference hGetEnumIndex;
        public MethodReference phReadMem;
        public MethodReference phWriteMem;
    }
}
