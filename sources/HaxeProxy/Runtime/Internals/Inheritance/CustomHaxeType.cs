
using Hashlink;
using Hashlink.Marshaling;
using Hashlink.Proxy.Objects;
using Hashlink.Reflection;
using Hashlink.Reflection.Types;
using ModCore.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HaxeProxy.Runtime.Internals.Inheritance
{
    internal unsafe class CustomHaxeType
    {
        public class ReflectType( HashlinkModule module, HL_type* type ) : 
            HashlinkObjectType(module, type)
        {
            public required CustomHaxeType CustomType
            {
                get; init;
            }
        }
        internal static readonly PinnedArrayList<FakeTypeData> fakeTypes = new();
        internal struct FakeTypeData
        {
            public HashlinkObject globalValue;
            public nint globalValuePtr;

            public nint typePtr;
            public Type type;
            public nint[] vproto;
            public nint[] methods;
            public HL_type hlType;
            public HL_type_obj hlObj;
            public HL_runtime_obj rtObj;
        }
        private readonly Dictionary<string, ProtoOverride> overrideMethodsDict = [];
        private nint fakeTypeDataPtr;
        public ref FakeTypeData Data => ref Unsafe.AsRef<FakeTypeData>((void*)fakeTypeDataPtr);

        public HL_type* nativeType;
        public HashlinkObjectType Type
        {
            get; private set;
        } = null!;

        public CustomHaxeType( Type type, HashlinkObjectType otype )
        {
            GenerateFakeTypeData(type, otype);

            Type curType = type;
            List<string> overrideMethods = [];
            while (!HaxeProxyManager.knownProxyTypes.Contains(curType))
            {
                foreach (var v in curType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    if (!v.IsVirtual)
                    {
                        continue;
                    }
                    overrideMethods.Add(v.Name);
                }
                Debug.Assert(curType.BaseType != null);
                curType = curType.BaseType;
            }
            foreach (var v in overrideMethods)
            {
                if (overrideMethodsDict.ContainsKey(v))
                {
                    continue;
                }
                var proto = otype.FindProto(v) ??
                    throw new MissingMethodException(otype.Name, v);
                var po = new ProtoOverride(proto, nativeType, curType.GetMethod(v) ??
                    throw new MissingMethodException(curType.FullName, v));
                overrideMethodsDict.Add(v, po);
            }
        }
        private void GenerateFakeTypeData( Type type, HashlinkObjectType otype )
        {
            ref var data = ref fakeTypes.Add(new());
            fakeTypeDataPtr = (nint)Unsafe.AsPointer(ref data);
            data.type = type;
            data.typePtr = (nint)Unsafe.AsPointer(ref data.hlType);
            nativeType = (HL_type*)data.typePtr;
            var src = otype.NativeType;
            var srcObj = otype.NativeType->data.obj;
            var srcRT = srcObj->rt;

            data.hlType = *src;
            data.hlType.data.obj = (HL_type_obj*)Unsafe.AsPointer(ref data.hlObj);

            data.hlObj = *srcObj;
            data.hlObj.super = src;
            data.hlObj.name = (char*)Marshal.StringToHGlobalUni(type.AssemblyQualifiedName);
            data.hlObj.rt = (HL_runtime_obj*)Unsafe.AsPointer(ref data.rtObj);

            data.rtObj = *srcRT;
            data.rtObj.parent = srcRT;


            data.methods = GC.AllocateArray<nint>(srcRT->nmethods, true);
            new ReadOnlySpan<nint>(srcRT->methods, srcRT->nmethods).CopyTo(data.methods);
            data.rtObj.methods = (void**)Unsafe.AsPointer(ref data.methods[0]);

            data.vproto = GC.AllocateArray<nint>(srcRT->nproto, true);
            new ReadOnlySpan<nint>(src->vobj_proto, srcRT->nproto).CopyTo(data.vproto);
            data.hlType.vobj_proto = (void**)Unsafe.AsPointer(ref data.vproto[0]);

            data.globalValue = new((HashlinkObjectType)otype.GlobalValue!.Type);
            data.globalValue.SetFieldValue("__name__", type.AssemblyQualifiedName);
            data.globalValue.SetFieldValue("__type__", (nint) nativeType);
            data.globalValue.SetFieldValue("__constructor__", (nint)0);
            data.globalValuePtr = data.globalValue.HashlinkPointer;
            data.hlObj.global_value = (void**)Unsafe.AsPointer(ref data.globalValuePtr);


            Type = new ReflectType(HashlinkMarshal.Module, nativeType)
            {
                CustomType = this
            };

        }
        

    }
}
