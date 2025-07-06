using dc.en;
using dc.pr;
using dc.shader;
using dc.tool;
using Serilog.Core;



namespace SampleSimple
{
    public static class Hook_WeaponCreate
    {
        public static Dictionary<string, Func<Hero, InventItem, Weapon>> WeaponCreateMap = new Dictionary<string, Func<Hero, InventItem, Weapon>>(); // 把对应关系写到这个表里面



        public delegate Weapon orig_create(Hero hero, InventItem item);
        public static Weapon Hook_create(orig_create orig, Hero hero, InventItem item)
        {
            // 先遍历查找映射表，没有合适的函数则使用原版逻辑
            if (WeaponCreateMap.TryGetValue(item._itemData.id.ToString(), out var creator))
            {
                return creator(hero, item);
            }
            else
            {
                return orig(hero, item);
            }
        }
    }
}
