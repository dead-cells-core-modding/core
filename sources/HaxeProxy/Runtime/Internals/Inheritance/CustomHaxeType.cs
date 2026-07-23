
using Hashlink;
using Hashlink.Marshaling;
using Hashlink.Marshaling.ObjHandle;
using Hashlink.Proxy;
using Hashlink.Proxy.Objects;
using Hashlink.Reflection;
using Hashlink.Reflection.Types;
using Hashlink.UnsafeUtilities;
using HaxeProxy.Events;
using ModCore.Collections;
using ModCore.Events;
using ModCore.Native.Events.Interfaces;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HaxeProxy.Runtime.Internals.Inheritance
{
    internal unsafe class CustomHaxeType
    {
        private class EventReceiver : IEventReceiver,
            IOnHashlinkDynGet,
            IOnHashlinkDynSet,
            IOnHashlinkDynHasField,
            IOnHashlinkCreateEmptyInstance
        {
            private static readonly MethodInfo MI_castObject = typeof(UtilityDelegates)
            .GetMethod(nameof(UtilityDelegates.CastObject), BindingFlags.Static | BindingFlags.NonPublic)!;
            private static readonly ConcurrentDictionary<Type, Func<object?, object?>> castDel = [];
            private static Func<object?, object?> GetCastDel( Type toType )
            {
                return castDel.GetOrAdd(toType, ( Type key ) =>
                {
                    var md = new DynamicMethod("dynamic_cast", typeof(object), [typeof(object)], true);
                    var il = md.GetILGenerator();

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Call, MI_castObject.MakeGenericMethod(toType));
                    il.Emit(OpCodes.Box, toType);
                    il.Emit(OpCodes.Ret);

                    return md.CreateDelegate<Func<object?, object?>>();
                });
            }
            public EventReceiver()
            {
                EventSystem.AddReceiver(this);
            }

            private bool TryGetField( HashlinkObject obj, int hfield,
                [NotNullWhen(true)] out FieldInfo? field, out object? finst )
            {
                var inst = obj.AsHaxe();

                finst = inst;

                Type ct;

                var reflectFlag = BindingFlags.Instance;

                if (obj.Type == ClassType)
                {
                    var meta = (HashlinkDynObj?)obj.GetFieldValue("__meta__");
                    if (meta == null ||
                        !meta.HasField("__DCCM_HaxeProxy_CustomType"))
                    {
                        field = null;
                        finst = null;
                        return false;
                    }
                    ref var data = ref Unsafe.AsRef<FakeTypeData>((void*)
                            (nint)meta.GetFieldValue("__DCCM_HaxeProxy_CustomType")!);

                    finst = null;
                    ct = data.type;
                    reflectFlag = BindingFlags.Static;
                }
                else if (obj.Type is ReflectType rt)
                {
                    ct = rt.CustomType.Data.type;
                }
                else
                {
                    field = null;
                    return false;
                }


                var hashedName = hfield;
                var fn = new string(HashlinkNative.hl_field_name(hashedName));

                if (fn == "???") //Unknown
                {
                    fn = "";
                    foreach (var f in ct.GetFields(BindingFlags.Public | BindingFlags.NonPublic
                        | reflectFlag))
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
                    field = null;
                    return false;
                }

                field = ct.GetField(fn, BindingFlags.Public | BindingFlags.NonPublic |
                      reflectFlag);
                if (field == null)
                {
                    return false;
                }
                return true;
            }

            EventResult<object?> IOnHashlinkDynGet.OnHashlinkDynGet( IOnHashlinkDynGet.Data data )
            {
                if (data.ptr == 0)
                {
                    return default;
                }
                var handle = HashlinkObjManager.TryGetHandle(data.ptr);
                if (handle == null ||
                    handle.Target == null)
                {
                    return default;
                }
                if (handle.Target is not HashlinkObject hobj)
                {
                    return default;
                }
                if (hobj.Type is not ReflectType rt &&
                    hobj.Type != ClassType)
                {
                    return default;
                }
                var tt = (HL_type*)data.ptype;

                if (!TryGetField(hobj, data.hfield, out var field, out var finst))
                {
                    return default;
                }
                var val = field.GetValue(finst);

                if (tt != null &&
                    tt->kind == TypeKind.HDYN)
                {
                    nint ptrBuf = 0;
                    HashlinkMarshal.WriteData(&ptrBuf, val, HashlinkMarshal.Module.KnownTypes.Dynamic);
                    return ptrBuf;
                }

                return val;
            }

            EventResult<bool> IOnHashlinkDynSet.OnHashlinkDynSet( IOnHashlinkDynSet.Data data )
            {
                if (data.ptr == 0)
                {
                    return default;
                }
                var handle = HashlinkObjManager.TryGetHandle(data.ptr);
                if (handle == null ||
                    handle.Target == null)
                {
                    return default;
                }
                if (handle.Target is not HashlinkObject hobj)
                {
                    return default;
                }
                if (hobj.Type is not ReflectType rt &&
                    hobj.Type != ClassType)
                {
                    return default;
                }

                if (!TryGetField(hobj, data.hfield, out var field, out var finst))
                {
                    return default;
                }

                nint val = 0;
                if (data.extraTypePtr.HasValue &&
                    data.val is nint vnint)
                {
                    val = vnint;
                }
                else
                {
                    HashlinkMarshal.WriteDataDyn(&val, data.val);
                }
                var rval = HashlinkMarshal.ConvertHashlinkObject(HashlinkObjPtr.Get(val));
                field.SetValue(finst,
                    GetCastDel(field.FieldType)(rval)
                    );
                return true;
            }

            EventResult<bool> IOnHashlinkDynHasField.OnHashlinkDynHasField( IOnHashlinkDynHasField.Data data )
            {
                if (data.ptr == 0)
                {
                    return default;
                }
                var handle = HashlinkObjManager.TryGetHandle(data.ptr);
                if (handle == null ||
                    handle.Target == null)
                {
                    return default;
                }
                if (handle.Target is not HashlinkObject hobj)
                {
                    return default;
                }
                if (hobj.Type is not ReflectType &&
                    hobj.Type != ClassType)
                {
                    return default;
                }

                return TryGetField(hobj, data.hfield, out var _, out var _);
            }

            EventResult<object> IOnHashlinkCreateEmptyInstance.OnHashlinkCreateEmptyInstance( HashlinkType type )
            {
                return default;
            }
        }

        private readonly static HashlinkObjectType ClassType = (HashlinkObjectType)HashlinkMarshal.Module.GetTypeByName("hl.Class");
        private readonly static EventReceiver er = new();

        public class ReflectType( HashlinkModule module, HL_type* type ) :
            HashlinkObjectType(module, type)
        {
            public required CustomHaxeType CustomType {
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

        public HL_type* nativeType;
        public HashlinkObjectType Type {
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
                var po = new ProtoOverride(proto, nativeType, curType.GetMethods()
                    .FirstOrDefault(x => x.Name == v && x.GetCustomAttribute<HashlinkAltAttribute>() == null) ??
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

            HashlinkObject classObj;

            {
                nint? classObjPtr = null;
                var classField = type.GetField("Class", BindingFlags.IgnoreCase | BindingFlags.Static | BindingFlags.Public |
                    BindingFlags.DeclaredOnly | BindingFlags.NonPublic);
                if (classField != null &&
                    classField.FieldType.IsAssignableTo(typeof(IHashlinkPointer)))
                {
                    classObjPtr = ((IHashlinkPointer?)classField.GetValue(null))?.HashlinkPointer;
                }
                else
                {
                    var classProp = type.GetProperty("Class", BindingFlags.IgnoreCase | BindingFlags.Static | BindingFlags.Public |
                    BindingFlags.DeclaredOnly | BindingFlags.NonPublic);
                    if (classProp != null &&
                        classProp.PropertyType.IsAssignableTo(typeof(IHashlinkPointer)))
                    {
                        classObjPtr = ((IHashlinkPointer?)classProp.GetValue(null))?.HashlinkPointer;
                    }
                }
                if (classObjPtr != null)
                {
                    var optr = HashlinkObjPtr.Get(classObjPtr.Value);
                    if (optr.Type->TypeName != "hl.Class" &&
                        optr.Type->data.obj->super->TypeName != "hl.Class")
                    {
                        throw new InvalidOperationException("The value of the `Class` field or property should inherit from `hl.Class`.");
                    }
                    classObj = (HashlinkObject)HashlinkMarshal.ConvertHashlinkObject(optr)!;
                }
                else
                {
                    classObj = new(ClassType);
                }
            }

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

            if (srcRT->nproto > 0)
            {
                data.vproto = GC.AllocateArray<nint>(srcRT->nproto, true);
                new ReadOnlySpan<nint>(src->vobj_proto, srcRT->nproto).CopyTo(data.vproto);
                data.hlType.vobj_proto = (void**)Unsafe.AsPointer(ref data.vproto[0]);
            }

            data.meta = new();
            data.meta.SetFieldValue("__DCCM_HaxeProxy_CustomType", (nint)Unsafe.AsPointer(ref data));

            data.globalValue = classObj;
            data.globalValue.SetFieldValue("__name__", type.AssemblyQualifiedName);
            data.globalValue.SetFieldValue("__type__", (nint)nativeType);
            data.globalValue.SetFieldValue("__constructor__", (nint)0);
            data.globalValue.SetFieldValue("__meta__", data.meta);



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
