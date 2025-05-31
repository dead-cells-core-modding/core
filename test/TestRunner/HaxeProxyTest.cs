using dc;
using dc.h2d.col;
using Hashlink.Virtuals;
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

            var e2 = new Boot.slowMoTweenieContext_0(114514, (dc.libs.misc.TType)dc.libs.misc.TType.Indexes.TZigZag,
                null);
            Assert.Equal(114514, e2.Param0);
            Assert.Equal(dc.libs.misc.TType.Indexes.TZigZag, e2.Param1.Index);
            Assert.Null(e2.Param2);
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
