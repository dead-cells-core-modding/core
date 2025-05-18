using Hashlink.Proxy.Objects;
using Hashlink.Proxy.Values;
using Hashlink;
using ModCore.Events.Interfaces.Game.Hero;
using ModCore.Mods;
using ModCore.Modules;
using Hashlink.Reflection.Types;
using Hashlink.Proxy.Clousre;
using Hashlink.Proxy.DynamicAccess;
using HaxeProxy.Runtime;
using dc.en;

namespace SampleHook
{
    public class SampleHookMod(ModInfo info) : ModBase(info),
        IOnHeroUpdate
    {
        private int AddHealth(int count, double ratio)
        {
            var hero = Game.Instance.HeroInstance;
            if (hero == null)
            {
                return 0;
            }
            var curLife = hero.life;
            var maxLife = hero.maxLife;
            var life = maxLife - curLife;
            if(life <= 0)
            {
                return 0;
            }
            int used;
            var val = (int)(count * ratio);
            if (val >= life)
            {
                curLife = maxLife;
                used = (int)(life / ratio);
            }
            else
            {
                curLife += val;
                used = count;
            }
            hero.setLifeAndRally(curLife, 5);
            return used;
        }
        private object? Hook_beheaded_addMoney(HashlinkClosure orig, HashlinkObject self, int val, nint noStats)
        {
            val -= AddHealth(val, 0.5f);
            return orig.DynamicInvoke(self, val * 10, noStats);
        }
        private object? Hook_beheaded_addCells(HashlinkClosure orig, HashlinkObject self, int val, nint noStats)
        {
            bool b = false;
            self.AsHaxe<Hero>().addMoney(val * 20, new(ref b));
            return orig.DynamicInvoke(self, val, noStats);
        }
        public override void Initialize()
        {
            HashlinkHooks.Instance.CreateHook("en.hero.Beheaded", "addMoney", Hook_beheaded_addMoney).Enable();
            HashlinkHooks.Instance.CreateHook("en.hero.Beheaded", "addCells", Hook_beheaded_addCells).Enable();
        }
        double timeDelt = 0;
        unsafe void IOnHeroUpdate.OnHeroUpdate(double dt)
        {
            var hero = Game.Instance.HeroInstance!;
            timeDelt += dt;

            if (timeDelt < 0.1f)
            {
                return;
            }
            var curLife = hero.life;
            var maxLife = hero.maxLife;
            var noStats = false;
            if (curLife < maxLife)
            {
                var addLife = hero.tryToSubstractMoney(20, new(ref noStats)) / 20;
                if(addLife > 0)
                {
                    hero.setLifeAndRally(curLife + addLife, 10);
                }
            }
            timeDelt = 0;
        }
    }
}
