using HaxeProxy.Runtime.Internals;
using Mono.Cecil;
using System;
using System.Linq;

namespace Haxe2CSharp
{
    internal class RuntimeHelperRef
    {
        public MethodReference delegateDynInvokeMethod;
        public MethodReference objectGetTypeMethod;
        public MethodReference typeGetFromHandleMethod;
        public MethodReference objBaseCtorMethod;

        public MethodReference attrFIndexCtor;
        public MethodReference attrTIndexCtor;

        public MethodReference phToVirtual;
        public MethodReference phGetNativeMethod;
        public MethodReference phDynGetMethod;
        public MethodReference phDynSetMethod;
        public MethodReference phCreateObject;
        public MethodReference phCreateClosure;
        public MethodReference phSetGlobal;
        public TypeReference objectBaseType;
        public MethodReference hGetEnumIndex;
        public MethodReference phReadMem;
        public MethodReference phWriteMem;
        public MethodReference hGetTypeIndexFromType;
        public MethodReference attrInitialValue;
        public MethodReference hGetNativeCall;
        public RuntimeHelperRef( ModuleDefinition module )
        {
            delegateDynInvokeMethod = ImportMethod(typeof(Delegate), nameof(Delegate.DynamicInvoke));
            objectGetTypeMethod = ImportMethod(typeof(object), nameof(GetType));
            typeGetFromHandleMethod = ImportMethod(typeof(Type), nameof(Type.GetTypeFromHandle));
            attrFIndexCtor = ImportAttribute<HashlinkFIndexAttribute>();
            attrTIndexCtor = ImportAttribute<HashlinkTIndexAttribute>();

            phToVirtual = ImportPseudocodeHelperMethod(nameof(PseudocodeHelper.ToVirtual));
            phGetNativeMethod = ImportPseudocodeHelperMethod(nameof(PseudocodeHelper.GetNativeMethod));
            phDynGetMethod = ImportPseudocodeHelperMethod(nameof(PseudocodeHelper.DynGet));
            phDynSetMethod = ImportPseudocodeHelperMethod(nameof(PseudocodeHelper.DynSet));
            phCreateObject = ImportPseudocodeHelperMethod(nameof(PseudocodeHelper.CreateObject));
            phCreateClosure = ImportPseudocodeHelperMethod(nameof(PseudocodeHelper.CreateClosure));
            phReadMem = ImportPseudocodeHelperMethod(nameof(PseudocodeHelper.ReadMem));
            phWriteMem = ImportPseudocodeHelperMethod(nameof(PseudocodeHelper.WriteMem));
            phSetGlobal = ImportPseudocodeHelperMethod(nameof(PseudocodeHelper.SetGlobal));

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

        }
    }
}
