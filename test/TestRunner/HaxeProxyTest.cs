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
    public class HaxeProxyTest
    {
        [Fact]
        public void Interaction_Object()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            double x = 114514;
            double y = 0;
            var p = new Point(new(ref x), new(ref y));

            Assert.Equal(x, p.x);
            Assert.Equal(y, p.y);

            p.normalize();

            Assert.Equal(1d, p.x);


        }

        [Fact]
        public void Interaction_Closure()
        {

            var d = new HaxeDynObj();
            dynamic dd = d;

            int val = 0;

            dd.x = (object)(() =>
            {
                val = 1;
            });

            var clx = (HashlinkClosure)dd.x;

            dd.x();

            Assert.Equal(1, val);

            dd.y = (object)((int v) =>
            {
                val = v;
            });

            dd.y(2);

            Assert.Equal(2, val);


        }

        [Fact]
        public unsafe void Interaction_Virtual()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var v = new virtual_file_line_s_
            {
                line = 114514,
                file = "Test".AsHaxeString()
            };

            Assert.Equal(114514, v.line);
            Assert.Equal("Test", v.file.ToString());

            var iter = new virtual_hasNext_next_<HlFunc<int>>();
            var iterType = iter.HashlinkObj.Type;

            var hlobj = HashlinkNative.hl_alloc_virtual(iterType.NativeType);
            var iter2 = ((HashlinkVirtual?)HashlinkMarshal.ConvertHashlinkObject(hlobj))?.AsHaxe();
            Assert.Equal(iterType, iter2?.HashlinkObj.Type);

            int v2v = 0;

            var v2 = new virtual_cb_inter_t_()
            {
                cb = () => v2v++
            };

            HashlinkClosure v2cl = v2.HashlinkObj.AsDynamic().cb;

            v2cl.DynamicInvoke();

            Assert.Equal(1, v2v);

            v2.cb();

            Assert.Equal(2, v2v);

        }
        [Fact]
        public void Interaction_Enum()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            var e = new Achievement_ID.BIOME_REACHED_SEWERS();

            Assert.NotNull(e);

            var e2 = new InventItemKind.Perk("A".AsHaxeString());
            Assert.Equal("A", e2.Param0.ToString());
            Assert.Equal(InventItemKind.Indexes.Perk, e2.Index);

            var et = HashlinkMarshal.Module.GetTypeByName("enum<AffectKeepChoice>") as HashlinkEnumType;
            Assert.NotNull(et);

            var inst1 = new HashlinkEnum(et, 1);
            Assert.Equal(1, inst1.Index);
            Assert.Equal(et, inst1.EnumType);

            var inst2 = inst1.AsHaxe();
            Assert.NotNull(inst2);
        }

        [Fact]
        public void Test_Dyn2()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            dynamic d = new HaxeDynObj();
            d.n = (object)((int? v) => (int?)v);

            Assert.Equal(1145, (int)d.n(1145));
            Assert.Null((int?)d.n(null));

            var del = (Func<int?, int?>)d.n;

            Assert.Equal(1145, del(1145));
            Assert.Null(del(null));

            var cl = (HashlinkClosure)d.n;

            Assert.Equal(1145, (int?)((dynamic)cl)(1145));
            Assert.Null((int?)((dynamic)cl)(null));

        }

        [Fact]
        public void Test_Dyn()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            double x = 114514;
            double y = 0;
            var p = new Point(new(ref x), new(ref y));

            var array = new ArrayObj()
            {
                array = new(HashlinkMarshal.Module.KnownTypes.Dynamic, 0)
            };
            array.push(p);
            Assert.Equal(array.pop(), p);

            var dyn = new HashlinkDynObj();
            dyn.SetFieldValue("test1", p);
            array.push(dyn);
            var dyn2 = array.pop();
            Assert.Equal((Point)dyn2.test1, p);

            array.pushDyn(114514);
            Assert.Equal(114514, (int)array.pop());
        }

        [Fact]
        public void Test_Native()
        {
            Assert.Equal(4, Lib_std.math_sqrt(16));
            Assert.Equal(1, Lib_std.math_abs(-1));
        }

        [Fact]
        public void Test_Hook()
        {
            HashlinkMarshal.EnsureThreadRegistered();

            double x = 114514;
            double y = 0;
            var p = new Point(new(ref x), new(ref y));

            Assert.Equal(x, p.x);
            Assert.Equal(y, p.y);

            Hook_Point.normalize += Hook_Point_normalize;

            p.normalize();

            Assert.Equal(0, p.x);
            Assert.Equal(x, p.y);

            p.x = x;
            p.y = y;

            Hook_Point.normalize -= Hook_Point_normalize;

            p.normalize();

            Assert.Equal(1, p.x);
            Assert.Equal(0, p.y);

        }

        [Fact]
        public void Test_Hook2()
        {
            var gm = HaxeProxyUtils.GetHashlinkType(typeof(Game)).CreateInstance().AsHaxe<Game>();


            Hook_Game.getBiomeVisitCount += Hook_Game_getBiomeVisitCount;
            var val = gm.getBiomeVisitCount("".AsHaxeString());
            Assert.Equal(11452007, val);
            
            Hook_Game.getBiomeVisitCount -= Hook_Game_getBiomeVisitCount;

            Hook_Game.getBiomeVisitCount += Hook_Game_getBiomeVisitCount2;

            val = gm.getBiomeVisitCount("".AsHaxeString());
            Assert.Null(val);

            Hook_Game.getBiomeVisitCount += Hook_Game_getBiomeVisitCount;

            val = gm.getBiomeVisitCount("".AsHaxeString());
            Assert.Equal(11452007, val);

            Hook_Game.getBiomeVisitCount += Hook_Game_getBiomeVisitCount3;

            val = gm.getBiomeVisitCount("".AsHaxeString());
            Assert.Equal(11452007 + 1, val);

            Hook_Game.getBiomeVisitCount -= Hook_Game_getBiomeVisitCount;
            Hook_Game.getBiomeVisitCount -= Hook_Game_getBiomeVisitCount2;
            Hook_Game.getBiomeVisitCount -= Hook_Game_getBiomeVisitCount3;

            Hook_Game.getBiomeVisitCount += Hook_Game_getBiomeVisitCount3;

            val = gm.getBiomeVisitCount("".AsHaxeString());
            Assert.Equal(0, val);

            Hook_Game.getBiomeVisitCount -= Hook_Game_getBiomeVisitCount3;
        }

        private int? Hook_Game_getBiomeVisitCount(Hook_Game.orig_getBiomeVisitCount orig, Game self, dc.String id)
        {
            return 11452007;
        }

        private int? Hook_Game_getBiomeVisitCount2(Hook_Game.orig_getBiomeVisitCount orig, Game self, dc.String id)
        {
            return null;
        }

        private int? Hook_Game_getBiomeVisitCount3(Hook_Game.orig_getBiomeVisitCount orig, Game self, dc.String id)
        {
            return (orig(self, id) ?? -1) + 1;
        }


        private void Hook_Point_normalize(Hook_Point.orig_normalize orig, Point self)
        {
            self.y = self.x;
            self.x = 0;
        }
    }
}
