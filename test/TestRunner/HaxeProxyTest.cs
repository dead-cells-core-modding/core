using dc;
using dc.h2d.col;
using dc.hl.types;
using dc.tool;
using Hashlink.Marshaling;
using Hashlink.Proxy.Objects;
using Hashlink.Proxy.Values;
using Hashlink.Reflection;
using Hashlink.Reflection.Types;
using Hashlink.Virtuals;
using HaxeProxy.Runtime;
using ModCore.Utitities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestMod;

namespace TestRunner
{
    public class HaxeProxyTest
    {
        [Fact]
        public void Interaction_Object()
        {
            double x = 114514;
            double y = 0;
            var p = new Point(new(ref x), new(ref y));

            Assert.Equal(x, p.x);
            Assert.Equal(y, p.y);

            p.normalize();

            Assert.Equal(1d, p.x);

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
        }
        [Fact]
        public void Interaction_Virtual()
        {
            var v = new virtual_fileName_lineNumber_
            {
                lineNumber = 114514,
                fileName = "Test".AsHaxeString()
            };

            Assert.Equal(114514, v.lineNumber);
            Assert.Equal("Test", v.fileName.ToString());
        }
        [Fact]
        public void Interaction_Enum()
        {
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
        public void Test_Hook()
        {
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

        private void Hook_Point_normalize(Hook_Point.orig_normalize orig, Point self)
        {
            self.y = self.x;
            self.x = 0;
        }
    }
}
