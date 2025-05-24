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
using dc.en.hero;

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
        private void Hook_beheaded_addMoney(Hook_Beheaded.orig_addMoney orig, Beheaded self, int val, Ref<bool> noStats)
        {
            val -= AddHealth(val, 0.5f);
            orig(self, val * 10, noStats);
        }
        private void Hook_beheaded_addCells(Hook_Beheaded.orig_addCells orig, Beheaded self, int val, Ref<bool> noStats)
        {
            bool b = false;
            self.addMoney(val * 20, new(ref b));
            orig(self, val, noStats);
        }
        public override void Initialize()
        {
            Hook_Beheaded.addMoney += Hook_beheaded_addMoney;
            Hook_Beheaded.addCells += Hook_beheaded_addCells;
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
