using dc;
using dc.en;
using dc.tool;
using dc.tool.weap;
using HaxeProxy.Runtime;
using ModCore.Storage;
using ModCore.Utitities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleSimple
{
    public class OtherDashSword : DashSword, IHxbitSerializable<object>
    {
        public static string name = "OtherDashSword";

        public OtherDashSword(Hero hero, InventItem item) : base(hero, item)
        {

        }

        // 测试效果——每帧增加10细胞
        public override void fixedUpdate()
        {
            base.fixedUpdate();
            bool noStats = false;
            this.owner.addCells(10, new HaxeProxy.Runtime.Ref<bool>(ref noStats));
        }

        object IHxbitSerializable<object>.GetData()
        {
            return new();
        }

        void IHxbitSerializable<object>.SetData(object data)
        {
            
        }
    }
}
