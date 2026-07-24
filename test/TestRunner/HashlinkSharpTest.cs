using System.Linq;

using Hashlink;
using Hashlink.Marshaling;
using Hashlink.Proxy;
using Hashlink.Proxy.Clousre;
using Hashlink.Proxy.DynamicAccess;
using Hashlink.Proxy.Objects;
using Hashlink.Proxy.Values;
using Hashlink.Reflection;
using Hashlink.Reflection.Members;
using Hashlink.Reflection.Members.Object;
using Hashlink.Reflection.Types;

namespace TestRunner
{
    public unsafe class HashlinkSharpTest
    {
        // =========================================================================
        // HashlinkModule Tests
        // =========================================================================

        [Fact]
        public void Module_GetTypeByName_PrimitiveTypes()
        {
            HashlinkMarshal.EnsureThreadRegistered();
            var m = HashlinkMarshal.Module;

            var i32Type = m.GetTypeByName("i32");
            Assert.NotNull(i32Type);
            Assert.Equal(TypeKind.HI32, i32Type.TypeKind);
            Assert.Equal("i32", i32Type.Name);

            var f64Type = m.GetTypeByName("f64");
            Assert.NotNull(f64Type);
            Assert.Equal(TypeKind.HF64, f64Type.TypeKind);

            var boolType = m.GetTypeByName("bool");
            Assert.NotNull(boolType);
            Assert.Equal(TypeKind.HBOOL, boolType.TypeKind);

            var voidType = m.GetTypeByName("void");
            Assert.NotNull(voidType);
            Assert.Equal(TypeKind.HVOID, voidType.TypeKind);

            var strType = m.GetTypeByName("String");
            Assert.NotNull(strType);
            Assert.Equal(TypeKind.HOBJ, strType.TypeKind);
        }

        [Fact]
        public void Module_TryGetTypeByName()
        {
            HashlinkMarshal.EnsureThreadRegistered();
            var m = HashlinkMarshal.Module;

            Assert.True(m.TryGetTypeByName("i32", out var type));
            Assert.NotNull(type);
            Assert.Equal(TypeKind.HI32, type.TypeKind);

            Assert.False(m.TryGetTypeByName("NonExistentType_XYZ123", out _));
        }

        [Fact]
        public void Module_KnownTypes_AllPresent()
        {
            HashlinkMarshal.EnsureThreadRegistered();
            var kt = HashlinkMarshal.Module.KnownTypes;

            Assert.NotNull(kt.String);
            Assert.NotNull(kt.Void);
            Assert.NotNull(kt.I32);
            Assert.NotNull(kt.I64);
            Assert.NotNull(kt.F32);
            Assert.NotNull(kt.F64);
            Assert.NotNull(kt.Bool);
            Assert.NotNull(kt.Bytes);
            Assert.NotNull(kt.Dynamic);
            Assert.NotNull(kt.Array);
            Assert.NotNull(kt.Type);
            Assert.NotNull(kt.DynObj);
        }

        [Fact]
        public void Module_GetFunctionByFIndex()
        {
            HashlinkMarshal.EnsureThreadRegistered();
            var m = HashlinkMarshal.Module;

            // There should be functions in the module
            Assert.NotEmpty(m.Functions);
            // GetFunctionByFIndex should return a valid function for existing index
            var firstFunc = m.Functions[0];
            Assert.NotNull(firstFunc);
            var func = m.GetFunctionByFIndex(firstFunc.FunctionIndex);
            Assert.NotNull(func);
            Assert.Equal(firstFunc.FunctionIndex, func.FunctionIndex);
        }

        [Fact]
        public void Module_IntsFloatsStrings()
        {
            HashlinkMarshal.EnsureThreadRegistered();
            var m = HashlinkMarshal.Module;

            Assert.NotNull(m.Ints);
            Assert.NotNull(m.Floats);
            Assert.NotNull(m.Strings);
            Assert.NotNull(m.Globals);
        }

        [Fact]
        public void Module_Types()
        {
            HashlinkMarshal.EnsureThreadRegistered();
            var m = HashlinkMarshal.Module;

            Assert.NotNull(m.Types);
            Assert.NotEmpty(m.Types);
            Assert.NotNull(m.PreferTypes);
            Assert.NotEmpty(m.PreferTypes);
        }

        // =========================================================================
        // HashlinkMarshal Tests
        // =========================================================================

        [Fact]
        public void Marshal_PrimitiveTypes_Dictionary()
        {
            HashlinkMarshal.EnsureThreadRegistered();
            var pt = HashlinkMarshal.PrimitiveTypes;

            Assert.Equal(typeof(int), pt[TypeKind.HI32]);
            Assert.Equal(typeof(long), pt[TypeKind.HI64]);
            Assert.Equal(typeof(ushort), pt[TypeKind.HUI16]);
            Assert.Equal(typeof(byte), pt[TypeKind.HUI8]);
            Assert.Equal(typeof(float), pt[TypeKind.HF32]);
            Assert.Equal(typeof(double), pt[TypeKind.HF64]);
            Assert.Equal(typeof(bool), pt[TypeKind.HBOOL]);
            Assert.Equal(typeof(void), pt[TypeKind.HVOID]);
            Assert.Equal(typeof(nint), pt[TypeKind.HBYTES]);
            Assert.Equal(typeof(nint), pt[TypeKind.HREF]);
            Assert.Equal(typeof(nint), pt[TypeKind.HTYPE]);
        }

        [Fact]
        public void Marshal_IsValueType()
        {
            Assert.True(TypeKind.HVOID.IsValueType());
            Assert.True(TypeKind.HI32.IsValueType());
            Assert.True(TypeKind.HI64.IsValueType());
            Assert.True(TypeKind.HF32.IsValueType());
            Assert.True(TypeKind.HF64.IsValueType());
            Assert.True(TypeKind.HBOOL.IsValueType());
            Assert.True(TypeKind.HBYTES.IsValueType());
            Assert.True(TypeKind.HREF.IsValueType());
            Assert.True(TypeKind.HTYPE.IsValueType());

            Assert.False(TypeKind.HOBJ.IsValueType());
            Assert.False(TypeKind.HDYN.IsValueType());
            Assert.False(TypeKind.HFUN.IsValueType());
            Assert.False(TypeKind.HARRAY.IsValueType());
        }

        [Fact]
        public void Marshal_IsPointer()
        {
            Assert.True(TypeKind.HBYTES.IsPointer());
            Assert.True(TypeKind.HOBJ.IsPointer());
            Assert.True(TypeKind.HDYN.IsPointer());
            Assert.True(TypeKind.HARRAY.IsPointer());
            Assert.True(TypeKind.HREF.IsPointer());
            Assert.True(TypeKind.HTYPE.IsPointer());
            Assert.True(TypeKind.HFUN.IsPointer());

            Assert.False(TypeKind.HVOID.IsPointer());
            Assert.False(TypeKind.HI32.IsPointer());
            Assert.False(TypeKind.HF64.IsPointer());
            Assert.False(TypeKind.HBOOL.IsPointer());
        }

        [Fact]
        public void Marshal_FindFunction_ExistingProto()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var func = HashlinkMarshal.FindFunction("h2d.col.Point", "normalize");
            Assert.NotNull(func);
            Assert.Equal("normalize", func.Name);
        }

        [Fact]
        public void Marshal_GetHashlinkType_ByNativeType()
        {
            HashlinkMarshal.EnsureThreadRegistered();
            var m = HashlinkMarshal.Module;

            var i32Type = m.GetTypeByName("i32");
            var resolved = HashlinkMarshal.GetHashlinkType(i32Type.NativeType);
            Assert.NotNull(resolved);
            Assert.Equal(TypeKind.HI32, resolved.TypeKind);
        }

        [Fact]
        public void Marshal_GetDyn_Int()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var dyn = HashlinkMarshal.GetDyn(42);
            Assert.NotEqual(0, dyn);
        }

        [Fact]
        public void Marshal_GetDyn_Float()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var dyn = HashlinkMarshal.GetDyn(3.14);
            Assert.NotEqual(0, dyn);
        }

        [Fact]
        public void Marshal_GetDyn_String()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var dyn = HashlinkMarshal.GetDyn("test string");
            Assert.NotEqual(0, dyn);
        }

        [Fact]
        public void Marshal_GetDyn_Null()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var dyn = HashlinkMarshal.GetDyn(null);
            Assert.Equal(0, dyn);
        }

        [Fact]
        public void Marshal_DefaultMarshaler_IsSet()
        {
            HashlinkMarshal.EnsureThreadRegistered();
            Assert.NotNull(HashlinkMarshal.DefaultMarshaler);
        }

        [Fact]
        public void Marshal_Module_IsPresent()
        {
            HashlinkMarshal.EnsureThreadRegistered();
            Assert.NotNull(HashlinkMarshal.Module);
        }

        // =========================================================================
        // HashlinkType Tests
        // =========================================================================

        [Fact]
        public void Type_TypeKind_MatchesKind()
        {
            HashlinkMarshal.EnsureThreadRegistered();
            var m = HashlinkMarshal.Module;

            Assert.Equal(TypeKind.HI32, m.GetTypeByName("i32").TypeKind);
            Assert.Equal(TypeKind.HI64, m.GetTypeByName("i64").TypeKind);
            Assert.Equal(TypeKind.HF32, m.GetTypeByName("f32").TypeKind);
            Assert.Equal(TypeKind.HF64, m.GetTypeByName("f64").TypeKind);
            Assert.Equal(TypeKind.HBOOL, m.GetTypeByName("bool").TypeKind);
            Assert.Equal(TypeKind.HVOID, m.GetTypeByName("void").TypeKind);
            Assert.Equal(TypeKind.HOBJ, m.GetTypeByName("String").TypeKind);
        }

        [Fact]
        public void Type_IsProperties()
        {
            HashlinkMarshal.EnsureThreadRegistered();
            var m = HashlinkMarshal.Module;

            var i32 = m.GetTypeByName("i32");
            Assert.True(i32.IsValueType);
            Assert.False(i32.IsPointer);
            Assert.False(i32.IsObject);
            Assert.False(i32.IsVirtual);
            Assert.False(i32.IsEnum);
            Assert.False(i32.IsArray);
            Assert.False(i32.IsDyn);

            var objType = m.GetTypeByName("String");
            Assert.True(objType.IsObject);
            Assert.True(objType.IsPointer);
            Assert.False(objType.IsValueType);
            Assert.False(objType.IsEnum);
        }

        [Fact]
        public void Type_SizeOf_Primitives()
        {
            HashlinkMarshal.EnsureThreadRegistered();
            var m = HashlinkMarshal.Module;

            Assert.Equal(4, m.GetTypeByName("i32").SizeOf);
            Assert.Equal(8, m.GetTypeByName("i64").SizeOf);
            Assert.Equal(8, m.GetTypeByName("f64").SizeOf);
            Assert.Equal(1, m.GetTypeByName("bool").SizeOf);
            Assert.Equal(0, m.GetTypeByName("void").SizeOf);
        }

        [Fact]
        public void Type_SizeOf_PointerTypes()
        {
            HashlinkMarshal.EnsureThreadRegistered();
            var m = HashlinkMarshal.Module;

            Assert.Equal(nint.Size, m.GetTypeByName("dynamic").SizeOf);
            Assert.Equal(nint.Size, m.GetTypeByName("bytes").SizeOf);
        }

        [Fact]
        public void Type_Name_IsSet()
        {
            HashlinkMarshal.EnsureThreadRegistered();
            var m = HashlinkMarshal.Module;

            Assert.Equal("i32", m.GetTypeByName("i32").Name);
            Assert.Equal("f64", m.GetTypeByName("f64").Name);
            Assert.Equal("bool", m.GetTypeByName("bool").Name);
            Assert.Equal("void", m.GetTypeByName("void").Name);
            Assert.Equal("String", m.GetTypeByName("String").Name);
        }

        [Fact]
        public void Type_TypeIndex_IsSet()
        {
            HashlinkMarshal.EnsureThreadRegistered();
            var m = HashlinkMarshal.Module;

            var t = m.GetTypeByName("i32");
            Assert.True(t.TypeIndex >= 0);
        }

        // =========================================================================
        // HashlinkArray Tests
        // =========================================================================

        [Fact]
        public void Array_CreateEmpty()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var arr = new HashlinkArray(HashlinkMarshal.Module.KnownTypes.I32, 0);
            Assert.NotNull(arr);
            Assert.True(arr.IsValid);
            Assert.Equal(0, arr.Count);
        }

        [Fact]
        public void Array_CreateAndAccessIntegers()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var arr = new HashlinkArray(HashlinkMarshal.Module.KnownTypes.I32, 5);
            Assert.NotNull(arr);
            Assert.True(arr.IsValid);
            Assert.Equal(5, arr.Count);
            Assert.Equal(HashlinkMarshal.Module.KnownTypes.I32, arr.ElementType);

            arr[0] = 10;
            arr[1] = 20;
            arr[2] = 30;
            arr[3] = 40;
            arr[4] = 50;

            Assert.Equal(10, (int)arr[0]!);
            Assert.Equal(20, (int)arr[1]!);
            Assert.Equal(30, (int)arr[2]!);
            Assert.Equal(40, (int)arr[3]!);
            Assert.Equal(50, (int)arr[4]!);
        }

        [Fact]
        public void Array_CreateAndAccessDoubles()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var arr = new HashlinkArray(HashlinkMarshal.Module.KnownTypes.F64, 3);
            arr[0] = 1.5;
            arr[1] = 2.5;
            arr[2] = 3.5;

            Assert.Equal(1.5, (double)arr[0]!);
            Assert.Equal(2.5, (double)arr[1]!);
            Assert.Equal(3.5, (double)arr[2]!);
        }

        [Fact]
        public void Array_CreateAndOverwrite()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var arr = new HashlinkArray(HashlinkMarshal.Module.KnownTypes.I32, 3);
            arr[0] = 100;
            arr[1] = 200;
            arr[2] = 300;

            // Overwrite
            arr[1] = 999;
            Assert.Equal(100, (int)arr[0]!);
            Assert.Equal(999, (int)arr[1]!);
            Assert.Equal(300, (int)arr[2]!);
        }

        [Fact]
        public void Array_DynamicAccess()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var arr = new HashlinkArray(HashlinkMarshal.Module.KnownTypes.F64, 3);
            arr[0] = 1.1;
            arr[1] = 2.2;
            arr[2] = 3.3;

            var dyn = arr.AsDynamic();
            Assert.NotNull(dyn);

            Assert.Equal(1.1, dyn[0]);
            Assert.Equal(2.2, dyn[1]);
            Assert.Equal(3.3, dyn[2]);

            dyn[1] = 99.9;
            Assert.Equal(99.9, dyn[1]);
        }

        [Fact]
        public void Array_AsSpan()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var arr = new HashlinkArray(HashlinkMarshal.Module.KnownTypes.I32, 4);
            arr[0] = 1;
            arr[1] = 2;
            arr[2] = 3;
            arr[3] = 4;

            var span = arr.AsSpan<int>();
            Assert.Equal(4, span.Length);
            Assert.Equal(1, span[0]);
            Assert.Equal(2, span[1]);
            Assert.Equal(3, span[2]);
            Assert.Equal(4, span[3]);
        }

        [Fact]
        public void Array_ElementTypeAndCount()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var arr = new HashlinkArray(HashlinkMarshal.Module.KnownTypes.F64, 7);
            Assert.Equal(7, arr.Count);
            Assert.Equal(HashlinkMarshal.Module.KnownTypes.F64, arr.ElementType);
        }

        // =========================================================================
        // HashlinkDynObj Tests
        // =========================================================================

        [Fact]
        public void DynObj_CreateAndSetGetFields()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var dyn = new HashlinkDynObj();
            Assert.NotNull(dyn);
            Assert.True(dyn.IsValid);

            dyn.SetFieldValue("myInt", 42);
            dyn.SetFieldValue("myFloat", 3.14);
            dyn.SetFieldValue("myBool", true);

            Assert.Equal(42, (int)dyn.GetFieldValue("myInt")!);
            Assert.Equal(3.14, (double)dyn.GetFieldValue("myFloat")!);
            Assert.True((bool)dyn.GetFieldValue("myBool")!);
        }

        [Fact]
        public void DynObj_DynamicMemberAccess()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var dyn = new HashlinkDynObj();
            var d = dyn.AsDynamic();
            Assert.NotNull(d);

            d.myStr = "hello dynamic";
            d.myNum = 123;

            Assert.Equal("hello dynamic", (string)d.myStr);
            Assert.Equal(123, (int) d.myNum);
        }

        [Fact]
        public void DynObj_SetFieldWithClosure()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var tt = (HashlinkFuncType)HashlinkMarshal.Module.GetTypeByName(
                "(void (dynamic))");

            bool wasCalled = false;
            var closure = new HashlinkClosure(tt, (object? _) =>
            {
                wasCalled = true;
            });

            var dyn = new HashlinkDynObj();
            dyn.SetFieldValue("myAction", closure);

            var retrieved = dyn.GetFieldValue("myAction") as HashlinkClosure;
            Assert.NotNull(retrieved);
            retrieved.DynamicInvoke([null]);
            Assert.True(wasCalled);
        }

        [Fact]
        public void DynObj_HasField()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var dyn = new HashlinkDynObj();
            Assert.False(dyn.HasField("testField"));

            dyn.SetFieldValue("testField", 42);
            Assert.True(dyn.HasField("testField"));
        }

        // =========================================================================
        // HashlinkString Tests
        // =========================================================================

        [Fact]
        public void String_CreateAndRead()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var str = new HashlinkString("Hello World");
            Assert.NotNull(str);
            Assert.True(str.IsValid);
            Assert.Equal("Hello World", str.TypedValue);
            Assert.Equal("Hello World", str.ToString());
        }

        [Fact]
        public void String_CreateEmpty()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var str = new HashlinkString("");
            Assert.NotNull(str);
            Assert.True(str.IsValid);
            Assert.Equal("", str.TypedValue);
        }

        [Fact]
        public void String_SetValue()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var str = new HashlinkString("initial");
            Assert.Equal("initial", str.TypedValue);

            str.TypedValue = "modified";
            Assert.Equal("modified", str.TypedValue);
        }

        [Fact]
        public void String_ValueProperty()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var str = new HashlinkString();
            str.Value = "via value property";
            Assert.Equal("via value property", str.TypedValue);
            Assert.Equal("via value property", (string)str.Value!);
        }

        // =========================================================================
        // HashlinkEnum Tests
        // =========================================================================

        [Fact]
        public void Enum_CreateDefault()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var et = HashlinkMarshal.Module.GetTypeByName("enum<AffectKeepChoice>") as HashlinkEnumType;
            Assert.NotNull(et);

            var inst = new HashlinkEnum(et, 0);
            Assert.NotNull(inst);
            Assert.True(inst.IsValid);
            Assert.Equal(0, inst.Index);
            Assert.Equal(et, inst.EnumType);
        }

        [Fact]
        public void Enum_Constructs()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var et = HashlinkMarshal.Module.GetTypeByName("enum<AffectKeepChoice>") as HashlinkEnumType;
            Assert.NotNull(et);

            var constructs = et.Constructs;
            Assert.NotNull(constructs);
            Assert.NotEmpty(constructs);
        }

        [Fact]
        public void Enum_CurrentConstruct()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var et = HashlinkMarshal.Module.GetTypeByName("enum<AffectKeepChoice>") as HashlinkEnumType;
            Assert.NotNull(et);

            var inst = new HashlinkEnum(et, 0);
            Assert.NotNull(inst.CurrentConstruct);
        }

        // =========================================================================
        // HashlinkObjectType Tests
        // =========================================================================

        [Fact]
        public void ObjectType_Fields()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var ot = (HashlinkObjectType)HashlinkMarshal.Module.GetTypeByName("h2d.col.Point");
            var fields = ot.Fields;
            Assert.NotNull(fields);
            Assert.NotEmpty(fields);
        }

        [Fact]
        public void ObjectType_Protos()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var ot = (HashlinkObjectType)HashlinkMarshal.Module.GetTypeByName("h2d.col.Point");
            var protos = ot.Protos;
            Assert.NotNull(protos);
            Assert.NotEmpty(protos);
        }

        [Fact]
        public void ObjectType_FindField_ByName()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var ot = (HashlinkObjectType)HashlinkMarshal.Module.GetTypeByName("h2d.col.Point");

            Assert.True(ot.HasField("x"));
            Assert.True(ot.HasField("y"));

            var fieldX = ot.FindField("x");
            Assert.NotNull(fieldX);
            Assert.Equal("x", fieldX.Name);

            var fieldY = ot.FindField("y");
            Assert.NotNull(fieldY);
            Assert.Equal("y", fieldY.Name);

            Assert.False(ot.HasField("nonExistentField_XYZ"));
            Assert.Null(ot.FindField("nonExistentField_XYZ"));
        }

        [Fact]
        public void ObjectType_TryFindField()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var ot = (HashlinkObjectType)HashlinkMarshal.Module.GetTypeByName("h2d.col.Point");

            Assert.True(ot.TryFindField("x", out var field));
            Assert.NotNull(field);
            Assert.Equal("x", field.Name);

            Assert.False(ot.TryFindField("doesntExist", out _));
        }

        [Fact]
        public void ObjectType_FindProto_ByName()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var ot = (HashlinkObjectType)HashlinkMarshal.Module.GetTypeByName("h2d.col.Point");

            Assert.True(ot.HasProto("normalize"));

            var proto = ot.FindProto("normalize");
            Assert.NotNull(proto);
            Assert.Equal("normalize", proto.Name);
        }

        [Fact]
        public void ObjectType_TryFindProto()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var ot = (HashlinkObjectType)HashlinkMarshal.Module.GetTypeByName("h2d.col.Point");

            Assert.True(ot.TryFindProto("normalize", out var proto));
            Assert.NotNull(proto);

            Assert.False(ot.TryFindProto("nonExistentProto", out _));
        }

        [Fact]
        public void ObjectType_Bindings()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var ot = (HashlinkObjectType)HashlinkMarshal.Module.GetTypeByName("h2d.col.Point");
            var bindings = ot.Bindings;
            Assert.NotNull(bindings);
        }

        [Fact]
        public void ObjectType_FieldIndex()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var ot = (HashlinkObjectType)HashlinkMarshal.Module.GetTypeByName("h2d.col.Point");
            var fields = ot.Fields;

            for (int i = 0; i < fields.Length; i++)
            {
                var fieldById = ot.FindFieldById(fields[i].Index);
                Assert.NotNull(fieldById);
                Assert.Equal(fields[i].Name, fieldById.Name);
            }
        }

        [Fact]
        public void ObjectType_TotalFieldsCount()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var ot = (HashlinkObjectType)HashlinkMarshal.Module.GetTypeByName("h2d.col.Point");
            Assert.True(ot.TotalFieldsCount >= ot.Fields.Length);
        }

        [Fact]
        public void ObjectType_FindProtoById()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var ot = (HashlinkObjectType)HashlinkMarshal.Module.GetTypeByName("h2d.col.Point");
            var protos = ot.Protos;

            foreach (var proto in protos)
            {
                var found = ot.FindProtoById(proto.ProtoIndex);
                Assert.NotNull(found);
                Assert.Equal(proto.Name, found.Name);
            }
        }

        [Fact]
        public void ObjectType_Super()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var ot = (HashlinkObjectType)HashlinkMarshal.Module.GetTypeByName("h2d.col.Point");
            // h2d.col.Point may or may not have a super, just test the property doesn't throw
            _ = ot.Super; // nullable, not asserting
        }

        // =========================================================================
        // HashlinkFuncType Tests
        // =========================================================================

        [Fact]
        public void FuncType_ArgTypesAndReturnType()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var tt = (HashlinkFuncType)HashlinkMarshal.Module.GetTypeByName(
                "(void (dynamic))");

            Assert.NotNull(tt);
            Assert.Equal(TypeKind.HFUN, tt.TypeKind);

            var argTypes = tt.ArgTypes;
            Assert.NotNull(argTypes);
            Assert.Single(argTypes);
            Assert.Equal(TypeKind.HDYN, argTypes[0].TypeKind);
        }

        [Fact]
        public void FuncType_CreateClosure()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var tt = (HashlinkFuncType)HashlinkMarshal.Module.GetTypeByName(
                "(void (dynamic))");

            bool wasCalled = false;
            var closure = new HashlinkClosure(tt, (object? _) =>
            {
                wasCalled = true;
            });

            Assert.NotNull(closure);
            closure.DynamicInvoke([null]);
            Assert.True(wasCalled);
        }

        // =========================================================================
        // HashlinkGlobal Tests
        // =========================================================================

        [Fact]
        public void Global_GetGlobalValue()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var logt = HashlinkMarshal.GetGlobal("haxe.Log");
            Assert.NotNull(logt);
        }

        [Fact]
        public void Global_IterateGlobals()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var globals = HashlinkMarshal.Module.Globals;
            Assert.NotNull(globals);
            Assert.NotEmpty(globals);

            foreach (var g in globals)
            {
                Assert.NotNull(g.Type);
                Assert.True(g.Index >= 0);
            }
        }

        [Fact]
        public void Global_TypeAndIndex()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var globals = HashlinkMarshal.Module.Globals;
            var first = globals[0];
            Assert.NotNull(first.Type);
            Assert.True(first.Index >= 0);
            Assert.NotNull(first.ToString());
        }

        // =========================================================================
        // HashlinkObj Tests
        // =========================================================================

        [Fact]
        public void Obj_IsValid_NewObject()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var obj = (HashlinkObject)HashlinkMarshal.Module
                .GetTypeByName("h2d.col.Point").CreateInstance();
            Assert.True(obj.IsValid);
        }

        [Fact]
        public void Obj_TypeKind()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var obj = (HashlinkObject)HashlinkMarshal.Module
                .GetTypeByName("h2d.col.Point").CreateInstance();
            Assert.Equal(TypeKind.HOBJ, obj.TypeKind);
        }

        [Fact]
        public void Obj_ToString()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var obj = (HashlinkObject)HashlinkMarshal.Module
                .GetTypeByName("h2d.col.Point").CreateInstance();
            var str = obj.ToString();
            Assert.NotNull(str);
            Assert.NotEmpty(str);
        }

        [Fact]
        public void Obj_MarkStateful()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var obj = (HashlinkObject)HashlinkMarshal.Module
                .GetTypeByName("h2d.col.Point").CreateInstance();
            obj.MarkStateful();
            // If it doesn't throw, it works
        }

        [Fact]
        public void Obj_Detach()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var obj = (HashlinkObject)HashlinkMarshal.Module
                .GetTypeByName("h2d.col.Point").CreateInstance();

            Assert.True(obj.IsValid);
            obj.Detach();
            Assert.False(obj.IsValid);
            Assert.Equal(0, obj.HashlinkPointer);
        }

        [Fact]
        public void Obj_NativeType()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var obj = (HashlinkObject)HashlinkMarshal.Module
                .GetTypeByName("h2d.col.Point").CreateInstance();
            Assert.True((nint)obj.NativeType != 0);
        }

        // =========================================================================
        // HashlinkObjPtr Tests
        // =========================================================================

        [Fact]
        public void ObjPtr_Get()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var obj = (HashlinkObject)HashlinkMarshal.Module
                .GetTypeByName("h2d.col.Point").CreateInstance();
            var ptr = HashlinkObjPtr.Get(obj.HashlinkPointer);

            Assert.Equal(obj.HashlinkPointer, ptr.Pointer);
            Assert.False(ptr.IsNull);
        }

        [Fact]
        public void ObjPtr_TypeKind()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var obj = (HashlinkObject)HashlinkMarshal.Module
                .GetTypeByName("h2d.col.Point").CreateInstance();
            var ptr = HashlinkObjPtr.Get(obj.HashlinkPointer);

            Assert.Equal(TypeKind.HOBJ, ptr.TypeKind);
        }

        [Fact]
        public void ObjPtr_GetMemSize()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var obj = (HashlinkObject)HashlinkMarshal.Module
                .GetTypeByName("h2d.col.Point").CreateInstance();
            var ptr = HashlinkObjPtr.Get(obj.HashlinkPointer);

            var memSize = ptr.GetMemSize();
            Assert.True(memSize > 0);
        }

        // =========================================================================
        // HashlinkFieldObject Tests
        // =========================================================================

        [Fact]
        public void FieldObject_HasField()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var inst = (HashlinkObject)HashlinkMarshal.Module
                .GetTypeByName("h2d.col.Point").CreateInstance();

            Assert.True(inst.HasField("x"));
            Assert.True(inst.HasField("y"));
            Assert.False(inst.HasField("nonExistent"));
        }

        [Fact]
        public void FieldObject_SetAndGetField_Double()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var inst = (HashlinkObject)HashlinkMarshal.Module
                .GetTypeByName("h2d.col.Point").CreateInstance();

            inst.SetFieldValue("x", 123.456);
            inst.SetFieldValue("y", 789.012);

            Assert.Equal(123.456, (double)inst.GetFieldValue("x")!);
            Assert.Equal(789.012, (double)inst.GetFieldValue("y")!);
        }

        [Fact]
        public void FieldObject_GetSetField_Int()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var inst = (HashlinkObject)HashlinkMarshal.Module
                .GetTypeByName("h2d.col.Point").CreateInstance();

            inst.SetFieldValue("x", 100);
            Assert.Equal(100.0, (double)inst.GetFieldValue("x")!);
        }

        [Fact]
        public void FieldObject_DynamicIndexer()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var inst = (HashlinkObject)HashlinkMarshal.Module
                .GetTypeByName("h2d.col.Point").CreateInstance();
            var dyn = inst.AsDynamic();

            dyn["x"] = 55.5;
            Assert.Equal(55.5, dyn["x"]);
        }

        [Fact]
        public void FieldObject_DynamicMember()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var inst = (HashlinkObject)HashlinkMarshal.Module
                .GetTypeByName("h2d.col.Point").CreateInstance();
            var dyn = inst.AsDynamic();

            dyn.x = 77.7;
            Assert.Equal(77.7, dyn.x);
        }

        // =========================================================================
        // HashlinkVirtual Tests
        // =========================================================================

        [Fact]
        public void Virtual_CreateFromType()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var logt = HashlinkMarshal.GetGlobal("haxe.Log");
            Assert.NotNull(logt);
            var trace = logt.GetFieldValue("trace") as HashlinkClosure;
            Assert.NotNull(trace);

            var tt = (HashlinkFuncType)trace.Type;
            var vt = (HashlinkVirtualType)tt.ArgTypes[1];

            var vinst = (HashlinkVirtual)vt.CreateInstance();
            Assert.NotNull(vinst);
            Assert.True(vinst.IsValid);

            // Set and get a field
            vinst.SetFieldValue("lineNumber", 42);
            Assert.Equal(42, (int)vinst.GetFieldValue("lineNumber")!);
        }

        [Fact]
        public void Virtual_GetValue()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var logt = HashlinkMarshal.GetGlobal("haxe.Log");
            Assert.NotNull(logt);
            var trace = logt.GetFieldValue("trace") as HashlinkClosure;
            Assert.NotNull(trace);

            var tt = (HashlinkFuncType)trace.Type;
            var vt = (HashlinkVirtualType)tt.ArgTypes[1];

            var vinst = (HashlinkVirtual)vt.CreateInstance();
            // GetValue may return null since no value was assigned
            _ = vinst.GetValue();
        }

        // =========================================================================
        // HashlinkNativeFunction Tests
        // =========================================================================

        [Fact]
        public void NativeFunction_FunctionIndex()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var funcs = HashlinkMarshal.Module.Functions;
            var natives = funcs.OfType<HashlinkNativeFunction>().ToArray();

            if (natives.Length > 0)
            {
                var native = natives[0];
                Assert.NotNull(native);
                Assert.True(native.FunctionIndex >= 0);
            }
        }

        // =========================================================================
        // HashlinkObject Tests (type-level)
        // =========================================================================

        [Fact]
        public void ObjectType_GlobalValue_GlobalType()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            // haxe.Log is a global type
            var logType = (HashlinkObjectType)HashlinkMarshal.Module.GetTypeByName("haxe.Log");
            var globalVal = logType.GlobalValue;
            Assert.NotNull(globalVal);
        }

        // =========================================================================
        // HashlinkMarshal Advanced Tests
        // =========================================================================

        [Fact]
        public void Marshal_IsAllocatedHashlinkObject()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var obj = (HashlinkObject)HashlinkMarshal.Module
                .GetTypeByName("h2d.col.Point").CreateInstance();

            Assert.True(HashlinkMarshal.IsAllocatedHashlinkObject((void*)obj.HashlinkPointer));
        }

        [Fact]
        public void Marshal_ConvertHashlinkObject_Null()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var result = HashlinkMarshal.ConvertHashlinkObject((void*)0);
            Assert.Null(result);
        }

        [Fact]
        public void Marshal_ConvertHashlinkObject_ObjPtr()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var obj = (HashlinkObject)HashlinkMarshal.Module
                .GetTypeByName("h2d.col.Point").CreateInstance();

            var ptr = HashlinkObjPtr.Get(obj.HashlinkPointer);
            var result = HashlinkMarshal.ConvertHashlinkObject(ptr);
            Assert.NotNull(result);
            Assert.IsType<HashlinkObject>(result);
        }

        [Fact]
        public void Marshal_ConvertHashlinkObject_Generic()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var obj = (HashlinkObject)HashlinkMarshal.Module
                .GetTypeByName("h2d.col.Point").CreateInstance();

            var result = HashlinkMarshal.ConvertHashlinkObject<HashlinkObject>(
                (void*)obj.HashlinkPointer);
            Assert.NotNull(result);
            Assert.Equal(obj.HashlinkPointer, result.HashlinkPointer);
        }

        [Fact]
        public void Marshal_MarkUsed()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var obj = (HashlinkObject)HashlinkMarshal.Module
                .GetTypeByName("h2d.col.Point").CreateInstance();

            // Should not throw
            HashlinkMarshal.MarkUsed(obj);
        }

        [Fact]
        public void Marshal_MarkStateful()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var obj = (HashlinkObject)HashlinkMarshal.Module
                .GetTypeByName("h2d.col.Point").CreateInstance();

            // Should not throw
            HashlinkMarshal.MarkStateful(obj);
        }

        [Fact]
        public void Marshal_EnsureThreadRegistered_ReturnsBool()
        {
            HashlinkMarshal.EnsureThreadRegistered();
            // First call should return true (new registration)

            Assert.NotNull(HashlinkThread.Current);
            Assert.False(HashlinkThread.EnsureThreadRegistered());

            // Second call should return false (already registered)
            var result2 = HashlinkMarshal.EnsureThreadRegistered();
            Assert.False(result2);
        }

        [Fact]
        public void Marshal_GetGlobal()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var logGlobal = HashlinkMarshal.GetGlobal("haxe.Log");
            Assert.NotNull(logGlobal);
            Assert.True(logGlobal.IsValid);
        }

        // =========================================================================
        // HashlinkType Edge Case Tests
        // =========================================================================

        [Fact]
        public void Type_IsEnum()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var et = HashlinkMarshal.Module.GetTypeByName("enum<AffectKeepChoice>");
            Assert.True(et.IsEnum);
            Assert.False(et.IsObject);
            Assert.False(et.IsVirtual);
        }

        [Fact]
        public void Type_IsAbstract()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            // Test that the property doesn't throw on various types
            var i32Type = HashlinkMarshal.Module.GetTypeByName("i32");
            Assert.False(i32Type.IsAbstract);
        }

        [Fact]
        public void Type_IsRef()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var i32Type = HashlinkMarshal.Module.GetTypeByName("i32");
            Assert.False(i32Type.IsRef);
        }

        [Fact]
        public void Type_IsNull()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var i32Type = HashlinkMarshal.Module.GetTypeByName("i32");
            Assert.False(i32Type.IsNull);
        }

        // =========================================================================
        // HashlinkFunction Tests
        // =========================================================================

        [Fact]
        public void Function_GetByFindFunction()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var func = HashlinkMarshal.FindFunction("h2d.col.Point", "normalize");
            Assert.NotNull(func);
            Assert.Equal("normalize", func.Name);
            Assert.True(func.FunctionIndex >= 0);
        }

        [Fact]
        public void Function_EntryPointer()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var func = HashlinkMarshal.FindFunction("h2d.col.Point", "normalize");
            Assert.NotEqual(0, func.EntryPointer);
        }

        [Fact]
        public void Function_FuncType()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var func = HashlinkMarshal.FindFunction("h2d.col.Point", "normalize");
            var funcType = func.FuncType;
            Assert.NotNull(funcType);
            Assert.Equal(TypeKind.HFUN, funcType.TypeKind);
        }

        [Fact]
        public void Function_CreateClosure()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var func = HashlinkMarshal.FindFunction("h2d.col.Point", "normalize");
            var closure = func.CreateClosure();
            Assert.NotNull(closure);
            Assert.True(closure.IsValid);
        }

        [Fact]
        public void Function_LocalRegisters()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var func = HashlinkMarshal.FindFunction("h2d.col.Point", "normalize");
            var regs = func.LocalRegisters;
            Assert.NotNull(regs);
        }

        [Fact]
        public void Function_CreateDelegate()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            // Find a simple function
            var funcs = HashlinkMarshal.Module.Functions;
            var hlFunc = funcs.OfType<HashlinkFunction>().FirstOrDefault();
            if (hlFunc != null)
            {
                var funcType = hlFunc.FuncType;
                if (funcType.ArgTypes.Length == 0 && funcType.ReturnType?.TypeKind == TypeKind.HVOID)
                {
                    // Safe to create delegate for void() functions
                    var del = hlFunc.CreateDelegate<Action>();
                    Assert.NotNull(del);
                }
            }
        }
    }
}
