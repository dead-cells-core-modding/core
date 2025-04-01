using Hashlink;
using Hashlink.Proxy;
using Hashlink.Proxy.Clousre;
using Hashlink.Proxy.Objects;
using Hashlink.Proxy.Values;
using Haxe;
using Haxe.Marshaling;
using ModCore.Events.Interfaces.Game;
using ModCore.Mods;
using ModCore.Modules;

namespace DoubleCells
{
    public class DoubleCellsMod(ModInfo info) : ModBase(info),
        IOnFrameUpdate
    {
        private double timeDelt = 0;
        private HaxeObject? hero;
        private object? Hook_hero_init(HashlinkClosure orig, HashlinkObject self)
        {
            hero = self.AsHaxe();
            return orig.DynamicInvoke(self);
        }
        private object? Hook_hero_dispose(HashlinkClosure orig, HashlinkObject self)
        {
            hero = null;
            return orig.DynamicInvoke(self);
        }
        private object? Hook_beheaded_addMoney(HashlinkClosure orig, HashlinkObject self, int val, nint noStats)
        {
            val *= 200;
            return orig.DynamicInvoke(self, val, noStats);
        }
        private object? Hook_beheaded_addCells(HashlinkClosure orig, HashlinkObject self, int val, nint noStats)
        {
            val *= 200;
            return orig.DynamicInvoke(self, val, noStats);
        }
        public override void Initialize()
        {
            Logger.Information("Hello, World!");
            HashlinkHooks.Instance.CreateHook("en.Hero", "init", Hook_hero_init).Enable();
            HashlinkHooks.Instance.CreateHook("en.Hero", "dispose", Hook_hero_dispose).Enable();
            HashlinkHooks.Instance.CreateHook("en.hero.Beheaded", "addMoney", Hook_beheaded_addMoney).Enable();
            HashlinkHooks.Instance.CreateHook("en.hero.Beheaded", "addCells", Hook_beheaded_addCells).Enable();
        }

        void IOnFrameUpdate.OnFrameUpdate(double dt)
        {
            timeDelt += dt;

            if (timeDelt < 0.25f || this.hero == null)
            {
                return;
            }
            var hero = this.hero.Chain;
            var curLife = (int)hero.life;
            var maxLife = (int)hero.maxLife;
            if (curLife < maxLife)
            {
                curLife += 1;
                hero.setLifeAndRally(curLife, 1);
            }
            timeDelt = 0;
        }
    }
}
