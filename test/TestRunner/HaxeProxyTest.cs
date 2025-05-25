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
            var p = new dc.h2d.col.Point(new(ref x), new(ref y));

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

            var e2 = new UnnamedEnum18556.Default(null, 1, 2, 3, 4, 5, 6, 7);
            Assert.Null(e2.Param0);
            Assert.Equal(1, e2.Param1);
            Assert.Equal(2, e2.Param2);
            Assert.Equal(3, e2.Param3);
            Assert.Equal(4, e2.Param4);
            Assert.Equal(5, e2.Param5);
            Assert.Equal(6, e2.Param6);
            Assert.Equal(7, e2.Param7);
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
