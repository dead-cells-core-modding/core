
using Hashlink;
using Hashlink.Marshaling;
using Hashlink.Proxy.Objects;
using Hashlink.Reflection;
using Hashlink.Reflection.Members.Object;
using Hashlink.Reflection.Types;
using ModCore.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
        internal static readonly ConcurrentDictionary<nint, CustomHaxeType> customTypes = new();
        internal struct FakeTypeData
        {
            public HashlinkObject globalValue;
            public nint globalValuePtr;
            public HashlinkDynObj meta;
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

        [UnmanagedCallersOnly]
        private static HL_vdynamic* NativeGetField( HL_vdynamic* obj, int hashedName )
        {
            var inst = (HaxeObject)HaxeProxyHelper.GetProxy<HaxeObject>(HashlinkMarshal.ConvertHashlinkObject(obj))!;
            var t = (ReflectType)inst.HashlinkObj.Type;
            var cht = t.CustomType;
            var ct = cht.Data.type;
            return HaxeGetField(inst, ct, hashedName, BindingFlags.Instance);
        }
        private static HL_vdynamic* HaxeGetField( HaxeObject? inst, 
            Type ct, int hashedName, BindingFlags flags )
        {
            
            var fn = new string(HashlinkNative.hl_field_name(hashedName));

            if (fn == "???") //Unknown
            {
                fn = "";
                foreach (var f in ct.GetFields(BindingFlags.Public | BindingFlags.NonPublic
                    | flags))
                {
                    fixed (char* name = f.Name)
                    {
                        var hash = HashlinkNative.hl_hash_gen(name, true);
                        if (hash == hashedName)
                        {
                            fn = f.Name;
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(fn))
            {
                throw new MissingFieldException(ct.FullName, hashedName.ToString());
            }

            var field = ct.GetField(fn, BindingFlags.Public | BindingFlags.NonPublic |
                 flags) ??
                throw new MissingFieldException(ct.FullName, fn);
            nint val = 0;
            HashlinkMarshal.WriteData(&val, field.GetValue(inst), HashlinkMarshal.Module.KnownTypes.Dynamic);
            return (HL_vdynamic*) val;
        }
        [UnmanagedCallersOnly]
        private static HL_vdynamic* NativeGetStaticField( HL_vdynamic* obj, int hashedName )
        {
            var inst = (HashlinkObject)HashlinkMarshal.ConvertHashlinkObject(obj)!;
            var meta = (HashlinkDynObj?) inst.GetFieldValue("__meta__");
            if (meta == null ||
                !meta.HasField("__DCCM_HaxeProxy_CustomType"))
            {
                return null;
            }
            ref var data = ref Unsafe.AsRef<FakeTypeData>((void*)
                    (nint)meta.GetFieldValue("__DCCM_HaxeProxy_CustomType")!);
            return HaxeGetField(null, data.type, hashedName, BindingFlags.Static);
        }


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

            customTypes.TryAdd(data.typePtr, this);

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

            data.rtObj.getFieldFun = 
                (delegate* unmanaged< HL_vdynamic*, int, HL_vdynamic* >)&NativeGetField;

            data.methods = GC.AllocateArray<nint>(srcRT->nmethods, true);
            new ReadOnlySpan<nint>(srcRT->methods, srcRT->nmethods).CopyTo(data.methods);
            data.rtObj.methods = (void**)Unsafe.AsPointer(ref data.methods[0]);

            data.vproto = GC.AllocateArray<nint>(srcRT->nproto, true);
            new ReadOnlySpan<nint>(src->vobj_proto, srcRT->nproto).CopyTo(data.vproto);
            data.hlType.vobj_proto = (void**)Unsafe.AsPointer(ref data.vproto[0]);

            data.meta = new();
            data.meta.SetFieldValue("__DCCM_HaxeProxy_CustomType", (nint)Unsafe.AsPointer(ref data));

            data.globalValue = new((HashlinkObjectType)otype.GlobalValue!.Type);
            data.globalValue.SetFieldValue("__name__", type.AssemblyQualifiedName);
            data.globalValue.SetFieldValue("__type__", (nint) nativeType);
            data.globalValue.SetFieldValue("__constructor__", (nint)0);
            data.globalValue.SetFieldValue("__meta__", data.meta);

            data.globalValue.Type.NativeType->data.obj->rt->getFieldFun = 
                    (delegate* unmanaged<HL_vdynamic*, int, HL_vdynamic*>) &NativeGetStaticField;

            data.globalValuePtr = data.globalValue.HashlinkPointer;
            data.hlObj.global_value = (void**)Unsafe.AsPointer(ref data.globalValuePtr);

            Type = new ReflectType(HashlinkMarshal.Module, nativeType)
            {
                CustomType = this
            };

            Debug.Assert(Type.GlobalValue == data.globalValue);
        }
        

    }
}
