using dc;
using dc.en;
using dc.en.inter;
using dc.tool;
using dc.tool.mod;
using ModCore.Events.Interfaces;
using ModCore.Events.Interfaces.Game;
using ModCore.Events.Interfaces.Game.Hero;
using ModCore.Mods;
using ModCore.Modules;
using ModCore.Utitities;
using Serilog;
using Serilog.Core;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static dc.en.mob.Variant;


namespace SampleSimple
{
    public class SimpleMod(ModInfo info) : ModBase(info),
        IOnHeroUpdate,
        IOnAfterLoadingAssets
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern short GetAsyncKeyState(int vkey);

        private static bool isLastFramePressed = false;
        
        private const int VK_OEM_5 = 0xDC; // 反斜杠键码

        public override void Initialize()
        {

            Logger.Information("Frostbite向你问候!");

            // 添加hook，修改create函数
            var hooks = HashlinkHooks.Instance;
            hooks.CreateHook("tool.$Weapon", "create", Hook_WeaponCreate.Hook_create).Enable();

            // 添加自己的string 到 构造函数映射表
            Hook_WeaponCreate.WeaponCreateMap.Add(OtherDashSword.name, (hero, item) => new OtherDashSword(hero, item));


        }

        void IOnHeroUpdate.OnHeroUpdate(double dt)
        {
            bool isCurrentFramePressed = (GetAsyncKeyState(VK_OEM_5) & 0x8000) != 0;
            Hero hero = Game.Instance.HeroInstance!;
            if (isCurrentFramePressed && !isLastFramePressed && hero != null )
            {   
                // 按下"\"之后执行效果 
                SpawnEffects(hero);
            }
            isLastFramePressed = isCurrentFramePressed;
        }

        // 在这里写代码效果
        private void SpawnEffects(Hero hero)
        {

            InventItem testItem = new InventItem(new InventItemKind.Weapon(OtherDashSword.name.AsHaxeString()));
            bool test_boolean = false;
            ItemDrop itemDrop = new ItemDrop(hero._level, hero.cx, hero.cy, testItem, true, new HaxeProxy.Runtime.Ref<bool>(ref test_boolean));
            // 生成掉落物后必须调用init方法，否则游戏会崩溃
            itemDrop.init();
            itemDrop.onDropAsLoot();
            itemDrop.dx = hero.dx; // 不知道为什么要有这一步，但是原版代码这么写的
        }

        void IOnAfterLoadingAssets.OnAfterLoadingAssets()
        {
            var res = Info.ModRoot!.GetFilePath("res.pak");
            FsPak.Instance.FileSystem.loadPak(res.AsHaxeString());
        }
    }
}
