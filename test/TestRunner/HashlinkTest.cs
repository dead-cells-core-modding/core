using Hashlink.Marshaling;
using Hashlink.Proxy;
using Hashlink.Proxy.Clousre;
using Hashlink.Proxy.DynamicAccess;
using Hashlink.Proxy.Objects;
using Hashlink.Proxy.Values;
using Hashlink.Reflection;
using Hashlink.Reflection.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.v3;

namespace TestRunner
{
    public class HashlinkTest
    {
        private static HashlinkObject CreateObject(string name)
        {
            return (HashlinkObject) HashlinkMarshal.Module.GetTypeByName(name).CreateInstance();
        }
        [Fact]
        public void Interaction_Object()
        {
            var inst = CreateObject("h2d.col.Point");

            inst.SetFieldValue("x", 114514d);
            Assert.Equal(114514d, inst.GetFieldValue("x"));

            var closure = inst.GetFieldValue("normalize") as HashlinkClosure;
            Assert.NotNull(closure);
            closure.DynamicInvoke();

            Assert.Equal(1d, inst.GetFieldValue("x"));

            //Dynamic

            var dyn = inst.AsDynamic();
            Assert.NotNull(dyn);

            dyn.x = 123456d;
            Assert.Equal(123456d, dyn.x);

            dyn.normalize();
            Assert.Equal(1d, dyn.x);
        }

        [Fact]
        public void Interaction_ObjectMarshal()
        {
            var inst = CreateObject("h2d.col.Edge");

            var ptr = inst.HashlinkPointer;
            Assert.Equal(inst, HashlinkMarshal.ConvertHashlinkObject(HashlinkObjPtr.Get(ptr)));

            var p1 = CreateObject("h2d.col.Point");
            inst.SetFieldValue("va", p1);
            Assert.Equal(p1, inst.GetFieldValue("va"));

        }

        [Fact]
        public void Interaction_Virtual()
        {
            var logt = HashlinkMarshal.GetGlobal("haxe.Log");
            Assert.NotNull(logt);

            var trace = logt.GetFieldValue("trace") as HashlinkClosure;
            Assert.NotNull(trace);

            var tt = (HashlinkFuncType)trace.Type;
            var vt = tt.ArgTypes[1];

            var vinst = (HashlinkVirtual) vt.CreateInstance();
            vinst.SetFieldValue("lineNumber", 114514);

            Assert.Equal(114514, vinst.GetFieldValue("lineNumber"));

            //Dynamic

            var dyn = vinst.AsDynamic();
            Assert.NotNull(dyn);

            dyn.lineNumber = 123456;
            Assert.Equal(123456, dyn.lineNumber);

        }

        [Fact]
        public void Interaction_Closure()
        {
            var tt = (HashlinkFuncType)HashlinkMarshal.Module.GetTypeByName("(void (dynamic,virtual<className:String,customParams:hl.types.ArrayDyn,fileName:String,lineNumber:i32,methodName:String>))");

            var isFailed = true;

            var cl = new HashlinkClosure(tt, (object? obj) =>
            {
                isFailed = false;
            });

            cl.DynamicInvoke([null]);
            Assert.False(isFailed);

            isFailed = true;
            cl.CreateDelegate<Action<object?>>()(null);
            Assert.False(isFailed);

            //Dynamic

            isFailed = true;

            var dyn = cl.AsDynamic();
            Assert.NotNull(dyn);

            dyn(null);
            Assert.False(isFailed);
        }

        [Fact]
        public void Interaction_Enum()
        {
            var et = HashlinkMarshal.Module.GetTypeByName("enum<AffectKeepChoice>") as HashlinkEnumType;
            Assert.NotNull(et);

            var inst1 = new HashlinkEnum(et, 0);
            Assert.Equal(0, inst1.Index);
            Assert.Equal(et, inst1.EnumType);
        }
    }
}
