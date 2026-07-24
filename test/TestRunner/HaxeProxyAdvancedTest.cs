using dc;
using dc.h2d.col;
using dc.hl.types;
using dc.pr;
using dc.tool;
using Hashlink;
using Hashlink.Marshaling;
using Hashlink.Proxy;
using Hashlink.Proxy.Clousre;
using Hashlink.Proxy.DynamicAccess;
using Hashlink.Proxy.Objects;
using Hashlink.Proxy.Values;
using Hashlink.Reflection.Types;
using Hashlink.Virtuals;
using HashlinkNET.Native.Impl;
using HaxeProxy.Runtime;
using HaxeProxy.Runtime.Internals;
using ModCore.Utilities;

namespace TestRunner
{
    /// <summary>
    /// Additional tests for HaxeProxy — proxy utilities, enum operations,
    /// virtual types, HaxeDynObj, HaxeNullable, Ref&lt;T&gt;, native functions,
    /// and dynamic access patterns not covered by the base HaxeProxyTest.
    /// </summary>
    public unsafe class HaxeProxyAdvancedTest
    {
        // =========================================================================
        // HaxeProxyUtils Tests
        // =========================================================================

        [Fact]
        public void Utils_GetHashlinkType_ReturnsCorrectType()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var ht = HaxeProxyUtils.GetHashlinkType(typeof(Point));
            Assert.NotNull(ht);
            Assert.Equal("h2d.col.Point", ht.Name);
            Assert.Equal(TypeKind.HOBJ, ht.TypeKind);
        }

        [Fact]
        public void Utils_GetProxyType_Roundtrip()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var ht = HashlinkMarshal.Module.GetTypeByName("h2d.col.Point");
            var proxyType = HaxeProxyUtils.GetProxyType(ht);
            Assert.NotNull(proxyType);
            Assert.Equal(typeof(Point), proxyType);
        }

        [Fact]
        public void Utils_AsHaxe_Roundtrip()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            double x = 10.0, y = 20.0;
            var p = new Point(new(ref x), new(ref y));

            var hlObj = p.HashlinkObj;
            var proxy = hlObj.AsHaxe<Point>();
            Assert.NotNull(proxy);
            Assert.Equal(10.0, proxy.x);
            Assert.Equal(20.0, proxy.y);
        }

        [Fact]
        public void Utils_AsObject()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            double x = 1.0, y = 2.0;
            var p = new Point(new(ref x), new(ref y));

            // AsObject should return the proxy itself
            var obj = p.HashlinkObj.AsObject();
            Assert.NotNull(obj);
            Assert.Same(p, obj);
        }

        // =========================================================================
        // HaxeProxyBase Tests
        // =========================================================================

        [Fact]
        public void Base_ToString_ReturnsNonEmpty()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            double x = 1.0, y = 2.0;
            var p = new Point(new(ref x), new(ref y));

            var str = p.ToString();
            Assert.NotNull(str);
            Assert.NotEmpty(str);
        }

        [Fact]
        public void Base_HashlinkPointer_MatchesUnderlying()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            double x = 1.0, y = 2.0;
            var p = new Point(new(ref x), new(ref y));

            Assert.NotEqual(0, p.HashlinkPointer);
            Assert.Equal(p.HashlinkObj.HashlinkPointer, p.HashlinkPointer);
        }

        // =========================================================================
        // Proxy Object Field Access Tests
        // =========================================================================

        [Fact]
        public void Object_CreateAndModifyFields()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            double x = 5.0, y = 6.0;
            var p = new Point(new(ref x), new(ref y));

            Assert.Equal(5.0, p.x);
            Assert.Equal(6.0, p.y);

            p.x = 100.0;
            p.y = 200.0;

            Assert.Equal(100.0, p.x);
            Assert.Equal(200.0, p.y);
        }

        [Fact]
        public void Object_MethodNormalize()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            double x = 3.0, y = 4.0;
            var p = new Point(new(ref x), new(ref y));

            p.normalize();

            Assert.Equal(0.6, p.x, 5);
            Assert.Equal(0.8, p.y, 5);
        }

        [Fact]
        public void Object_DynamicFieldAccess()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            double x = 1.0, y = 2.0;
            var p = new Point(new(ref x), new(ref y));

            dynamic dp = p;
            Assert.Equal(1.0, dp.x);
            Assert.Equal(2.0, dp.y);

            dp.x = 99.0;
            Assert.Equal(99.0, dp.x);
        }

        [Fact]
        public void Object_DynamicMethodCall()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            double x = 3.0, y = 4.0;
            var p = new Point(new(ref x), new(ref y));

            dynamic dp = p;
            dp.normalize();

            Assert.Equal(0.6, dp.x, 5);
        }

        // =========================================================================
        // Proxy Enum Tests
        // =========================================================================

        [Fact]
        public void Enum_CreateSimpleVariant()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var e = new Achievement_ID.BIOME_REACHED_SEWERS();
            Assert.NotNull(e);
            Assert.NotNull(e.HashlinkObj);
            Assert.True(e.HashlinkObj.IsValid);
        }

        [Fact]
        public void Enum_CreateVariantWithParam()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var e = new InventItemKind.Perk("TestPerk".AsHaxeString());
            Assert.Equal("TestPerk", e.Param0.ToString());
            Assert.Equal(InventItemKind.Indexes.Perk, e.Index);
        }

        [Fact]
        public void Enum_Equality_SameVariant()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var e1 = new Achievement_ID.BIOME_REACHED_SEWERS();
            var e2 = new Achievement_ID.BIOME_REACHED_SEWERS();

            Assert.True(e1.Equals(e2));
            Assert.True(e1 == e2);
        }

        [Fact]
        public void Enum_GetHashCode_Consistent()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var e1 = new Achievement_ID.BIOME_REACHED_SEWERS();
            var e2 = new Achievement_ID.BIOME_REACHED_SEWERS();

            Assert.Equal(e1.GetHashCode(), e2.GetHashCode());
        }

        [Fact]
        public void Enum_ToString_ReturnsName()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var e = new Achievement_ID.BIOME_REACHED_SEWERS();
            var str = e.ToString();
            Assert.NotNull(str);
            Assert.NotEmpty(str);
        }

        [Fact]
        public void Enum_HashlinkEnumRoundtrip()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var et = HashlinkMarshal.Module.GetTypeByName("enum<AffectKeepChoice>") as HashlinkEnumType;
            Assert.NotNull(et);

            var hlEnum = new HashlinkEnum(et, 1);
            var proxy = hlEnum.AsHaxe();
            Assert.NotNull(proxy);
        }

        // =========================================================================
        // Proxy Virtual Tests
        // =========================================================================

        [Fact]
        public void Virtual_CreateAndSetIntField()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var v = new virtual_file_line_s_
            {
                line = 42,
                file = "myfile.hx".AsHaxeString()
            };

            Assert.Equal(42, v.line);
            Assert.Equal("myfile.hx", v.file.ToString());
        }

        [Fact]
        public void Virtual_ModifyFields()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var v = new virtual_file_line_s_
            {
                line = 10,
                file = "old.hx".AsHaxeString()
            };

            v.line = 99;
            v.file = "new.hx".AsHaxeString();

            Assert.Equal(99, v.line);
            Assert.Equal("new.hx", v.file.ToString());
        }

        [Fact]
        public void Virtual_GenericTypeCreation()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var iter = new virtual_hasNext_next_<HlFunc<int>>();
            Assert.NotNull(iter);
            Assert.NotNull(iter.HashlinkObj);
            Assert.True(iter.HashlinkObj.IsValid);
        }

        [Fact]
        public void Virtual_WithClosureField()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            int callCount = 0;
            var v = new virtual_cb_inter_t_()
            {
                cb = () => callCount++
            };

            Assert.Equal(0, callCount);
            v.cb();
            Assert.Equal(1, callCount);
            v.cb();
            Assert.Equal(2, callCount);
        }

        [Fact]
        public void Virtual_DynamicFieldAccess()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var v = new virtual_file_line_s_
            {
                line = 50,
                file = "dynamic.hx".AsHaxeString()
            };

            dynamic dv = v;
            Assert.Equal(50, dv.line);
            Assert.Equal("dynamic.hx", dv.file.ToString());

            dv.line = 100;
            Assert.Equal(100, dv.line);
        }

        // =========================================================================
        // HaxeDynObj Tests
        // =========================================================================

        [Fact]
        public void DynObj_CreateAndSetTypedFields()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var d = new HaxeDynObj();
            dynamic dd = d;

            dd.intVal = 42;
            dd.strVal = "hello";
            dd.floatVal = 3.14;

            Assert.Equal(42, dd.intVal);
            Assert.Equal("hello", (string)dd.strVal);
            Assert.Equal(3.14, dd.floatVal);
        }

        [Fact]
        public void DynObj_StoreAndInvokeClosure()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var d = new HaxeDynObj();
            dynamic dd = d;

            int sum = 0;
            dd.add = (object)((int a, int b) => sum = a + b);

            dd.add(10, 20);
            Assert.Equal(30, sum);

            dd.add(5, 7);
            Assert.Equal(12, sum);
        }

        [Fact]
        public void DynObj_MultipleClosures()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var d = new HaxeDynObj();
            dynamic dd = d;

            int lastOp = 0;
            dd.multiply = (object)((int a, int b) => lastOp = a * b);
            dd.add = (object)((int a, int b) => lastOp = a + b);

            dd.multiply(5, 6);
            Assert.Equal(30, lastOp);

            dd.add(5, 6);
            Assert.Equal(11, lastOp);
        }
     

        // =========================================================================
        // Ref&lt;T&gt; Tests
        // =========================================================================

        [Fact]
        public void Ref_CreateAndRead()
        {
            int val = 42;
            var r = Ref<int>.From(ref val);
            Assert.Equal(42, r.value);
        }

        [Fact]
        public void Ref_WriteThrough()
        {
            int val = 42;
            var r = Ref<int>.From(ref val);
            r.value = 99;
            Assert.Equal(99, val);
        }

        [Fact]
        public void Ref_Null_IsNull()
        {
            var r = Ref<int>.Null;
            Assert.True(r.IsNull);
        }

        [Fact]
        public void Ref_DontCare_NotNull()
        {
            var r = Ref<int>.DontCare;
            Assert.False(r.IsNull);
            Assert.Equal(0, r.value);
        }

        [Fact]
        public void Ref_In_PreservesValue()
        {
            int val = 123;
            var r = Ref<int>.In(in val);
            Assert.False(r.IsNull);
            Assert.Equal(123, r.value);
        }

        // =========================================================================
        // Native Function Tests
        // =========================================================================

        [Fact]
        public void Native_Sqrt_ExactValues()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            Assert.Equal(4, Lib_std.math_sqrt(16));
            Assert.Equal(5, Lib_std.math_sqrt(25));
            Assert.Equal(0, Lib_std.math_sqrt(0));
        }

        [Fact]
        public void Native_Abs_NegativeAndPositive()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            Assert.Equal(5, Lib_std.math_abs(-5));
            Assert.Equal(3, Lib_std.math_abs(3));
            Assert.Equal(0, Lib_std.math_abs(0));
        }

        // =========================================================================
        // Dynamic Access Edge Cases
        // =========================================================================

        [Fact]
        public void Dynamic_ProxyToString()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            double x = 1.0, y = 2.0;
            var p = new Point(new(ref x), new(ref y));

            dynamic dp = p;
            string str = dp;
            Assert.NotNull(str);
        }

        [Fact]
        public void Dynamic_DynObjViaHashlinkDynObj()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var hldyn = new HashlinkDynObj();
            hldyn.SetFieldValue("key1", "value1");
            hldyn.SetFieldValue("key2", 123);

            var proxy = hldyn.AsHaxe<HaxeDynObj>();
            Assert.NotNull(proxy);

            dynamic dp = proxy;
            Assert.Equal("value1", (string)dp.key1);
            Assert.Equal(123, dp.key2);
        }

        // =========================================================================
        // AsHaxeString Tests
        // =========================================================================

        [Fact]
        public void AsHaxeString_CreateAndRead()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var hs = "Hello".AsHaxeString();
            Assert.NotNull(hs);
            Assert.Equal("Hello", hs.ToString());
        }

        [Fact]
        public void AsHaxeString_EmptyString()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var hs = "".AsHaxeString();
            Assert.NotNull(hs);
            Assert.Equal("", hs.ToString());
        }

        [Fact]
        public void AsHaxeString_UsedInObjectField()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var v = new virtual_file_line_s_
            {
                line = 1,
                file = "testfile.hx".AsHaxeString()
            };

            Assert.Equal("testfile.hx", v.file.ToString());
        }

        // =========================================================================
        // ArrayObj Tests (dynamic array operations)
        // =========================================================================

        [Fact]
        public void ArrayObj_PushPop()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            double x = 1.0, y = 2.0;
            var p = new Point(new(ref x), new(ref y));

            var arr = new ArrayObj()
            {
                array = new(HashlinkMarshal.Module.KnownTypes.Dynamic, 0)
            };

            arr.push(p);
            var popped = arr.pop();
            Assert.Equal(p, popped);
        }

        [Fact]
        public void ArrayObj_PushDynAndPop()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var arr = new ArrayObj()
            {
                array = new(HashlinkMarshal.Module.KnownTypes.Dynamic, 0)
            };

            arr.pushDyn(42);
            arr.pushDyn(99);

            Assert.Equal(99, (int)arr.pop());
            Assert.Equal(42, (int)arr.pop());
        }
    }
}
